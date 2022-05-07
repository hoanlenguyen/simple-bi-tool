using BITool.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using MySql.Data.MySqlClient;
using System.Data;

namespace BITool.Services
{
    public static class ExportDataService
    {
        public static void AddExportDataService(this WebApplication app, string sqlConnectionStr)
        {
            app.MapGet("data/getCustomers", [Authorize]
            async Task<IResult>
                (
                //IConfiguration configuration,
                int? scoreId,
                string? scoreCategory,
                string? keyWord,
                DateTime? dateFirstAddedFrom,
                DateTime? dateFirstAddedTo
                ) =>
            {
                //var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];
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

                    var customerData = conn.Query</*CustomerDto*/ string>(query)
                                                    .ToList();
                    return Results.Ok(customerData);
                }
            });

            app.MapGet("data/getCustomersBySP", [Authorize]
            async Task<IResult>
                (
                //IConfiguration configuration,
                int? scoreId,
                string? scoreCategory,
                string? keyWord,
                DateTime? dateFirstAddedFrom,
                DateTime? dateFirstAddedTo
                ) =>
            {
                //var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];

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
                        customerData.Add((string)rdr["CustomerMobileNo"]);
                    }
                    rdr.Close();
                    conn.Close();
                    return Results.Ok(customerData.Count);
                }
            });
        }
    }
}