using BITool.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text;

namespace BITool.Services
{
    public static class ExportDataService
    {
        #region private
        private static void BulkInsertCustomerCampaignToMySQL(string sqlConnectionStr, IEnumerable<string> customerMobileList, int campaignID)
        {
            var nowStr = DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss");            
            var sCommand = new StringBuilder("INSERT IGNORE INTO recordcustomerexport (CustomerMobileNo, CampaignNameID, DateExported, Status) VALUES ");
            using (MySqlConnection connection = new MySqlConnection(sqlConnectionStr))
            {
                List<string> Rows = new List<string>();
                foreach (var phone in customerMobileList)
                {
                    Rows.Add(string.Format("('{0}',{1}, '{2}', {3})",
                        MySqlHelper.EscapeString(phone),
                        campaignID,
                        MySqlHelper.EscapeString(nowStr),
                        1
                        ));
                }

                sCommand.Append(string.Join(",", Rows));
                sCommand.Append(";");
                connection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }
        #endregion private
        public static void AddExportDataService(this WebApplication app, string sqlConnectionStr)
        {
            app.MapGet("data/getCustomers", [Authorize]
            async Task<IResult>
                (
                int? scoreId,
                string? scoreCategory,
                string? keyWord,
                DateTime? dateFirstAddedFrom,
                DateTime? dateFirstAddedTo
                ) =>
            {
                using (var conn = new MySqlConnection(sqlConnectionStr))
                {
                    var query = "SELECT c.CustomerMobileNo " +
                        //", c.CustomerMobileNo , c.DateFirstAdded , c.Status " +
                        "FROM customer c " +
                        "INNER JOIN customerscore cs " +
                        "ON c.CustomerMobileNo = cs.CustomerMobileNo " +
                        "LEFT JOIN adminscore ads " +
                        "ON ads.ScoreID = cs.ScoreID " +
                        "WHERE true ";

                    if (scoreId != null)
                        query += $"AND cs.ScoreID = {scoreId} ";

                    if (!string.IsNullOrEmpty(scoreCategory))
                        query += $"AND ads.ScoreCategory = '{scoreCategory}' ";

                    if (!string.IsNullOrEmpty(keyWord))
                        query += $"AND c.CustomerMobileNo LIKE '%{keyWord}%' ";

                    if (dateFirstAddedFrom != null)
                    {
                        var fromDate = dateFirstAddedFrom.Value.Date.AddSeconds(-1).ToString("yyyy-MM-dd hh:mm:ss");
                        query += $"AND c.DateFirstAdded > '{fromDate}' ";
                    }

                    if (dateFirstAddedTo != null)
                    {
                        var toDate = dateFirstAddedTo.Value.Date.AddSeconds(1).ToString("yyyy-MM-dd hh:mm:ss");
                        query += $"AND c.DateFirstAdded < '{toDate}' ";
                    }

                    //query += " LIMIT 1000";

                    var customerData = conn.Query<string>(query)
                                                    .ToList();
                    return Results.Ok(customerData);
                }
            });

            app.MapGet("data/getCustomersBySP", [AllowAnonymous]
            async Task<IResult>
                (
                int? scoreId,
                string? scoreCategory,
                string? keyWord,
                DateTime? dateFirstAddedFrom,
                DateTime? dateFirstAddedTo
                ) =>
            {
                if (dateFirstAddedFrom != null)
                    dateFirstAddedFrom = dateFirstAddedFrom.Value.Date;

                if (dateFirstAddedTo != null)
                    dateFirstAddedTo = dateFirstAddedTo.Value.Date.AddDays(1).AddMilliseconds(-1);

                using (var conn = new MySqlConnection(sqlConnectionStr))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SearchCustomer", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@scoreId", scoreId);
                    cmd.Parameters.AddWithValue("@scoreCategory", scoreCategory);
                    cmd.Parameters.AddWithValue("@keyWord", keyWord);
                    cmd.Parameters.AddWithValue("@dateFirstAddedFrom", dateFirstAddedFrom);
                    cmd.Parameters.AddWithValue("@dateFirstAddedTo", dateFirstAddedTo);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    var customerData = new List<string>();
                    while (rdr.Read())
                    {
                        customerData.Add(rdr.GetString(0));
                    }
                    rdr.Close();
                    conn.Close();
                    return Results.Ok(customerData);
                }
            });

            app.MapPost("data/assignCampaignToCustomers", [Authorize]
            async Task<IResult>(AssignCampaignToCustomerModel input) =>
            {
                if(input == null)
                    return Results.BadRequest("No input data");

                if (input.CampaignID == 0)
                    return Results.BadRequest("No selected CampaignID");

                BulkInsertCustomerCampaignToMySQL(sqlConnectionStr, input.CustomerList, input.CampaignID);

                return Results.Ok(input);
            });
        }
    }
}