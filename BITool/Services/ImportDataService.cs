using BITool.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System.Data;
using System.Globalization;
using System.Text;

namespace BITool.Services
{
    public static class ImportDataService
    {
        #region Private
        private const string GetAdminScoresKey = "getAdminScores";
        private const string GetAdminCampaignsKey = "getAdminCampaigns";
        private static DateTime? CheckValidDate(string input)
        {
            DateTime result;
            var checkParse = DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd/MM/yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MM-yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MM-yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            //if (!checkParse)
            //    checkParse = DateTime.TryParseExact(input, "dd-MMM-yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            //if (!checkParse)
            //    checkParse = DateTime.TryParseExact(input, "dd-MMM-yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            //if (!checkParse)
            //    checkParse = DateTime.TryParse(input, out result);

            if (!checkParse)
                return null;

            return result;
        }

        private static bool CheckValidPhoneNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Length != 9)
                return false;

            var firstChar = input[0];
            if (firstChar != '6' && firstChar != '8' && firstChar != '9')
                return false;

            if (!int.TryParse(input, out int result))
                return false;

            return true;
        }

        private static void BulkInsertCustomerModelToMySQL(string sqlConnectionStr, IEnumerable<string> customerRows)
        {
            //add INSERT IGNORE to avoid throw error when duplicate CustomerMobileNo
            var sCommand = new StringBuilder("INSERT IGNORE INTO customer (DateFirstAdded, CustomerMobileNo, Status) VALUES ");
            using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
            {
                sCommand.Append(string.Join(",", customerRows)); //may use MySqlHelper.EscapeString
                sCommand.Append(";");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }

        private static void BulkInsertCustomerScoreToMySQL(string sqlConnectionStr, IEnumerable<string> items)
        {
            var sCommand = new StringBuilder(
                "INSERT IGNORE INTO customerscore (CustomerMobileNo, ScoreID, DateOccurred, Status) VALUES ");
            using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
            {
                sCommand.Append(string.Join(",", items));
                sCommand.Append(";");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }

        private static List<AdminScoreDto> GetAdminScores(string sqlConnectionStr)
        {
            using var connection = new MySqlConnection(sqlConnectionStr);
            return connection.Query<AdminScoreDto>("SELECT * FROM adminscore").ToList();
        }
        private static List<AdminCampaignDto> GetAdminCampaigns(string sqlConnectionStr)
        {
            using var connection = new MySqlConnection(sqlConnectionStr);
            return connection.Query<AdminCampaignDto>("SELECT * FROM admincampaign").ToList();
        }
        #endregion Private

        public static void AddImportDataService(this WebApplication app, string sqlConnectionStr)
        {
            app.MapGet("data/getAdminCampaigns", [Authorize] async Task<IResult> (IMemoryCache memoryCache) =>
            {
                List<AdminCampaignDto> items = null;
                if (memoryCache.TryGetValue(GetAdminCampaignsKey, out items))
                    return Results.Ok(items);

                items = GetAdminCampaigns(sqlConnectionStr);
                var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                memoryCache.Set(GetAdminCampaignsKey, items, cacheOptions);
                return Results.Ok(items);
            });

            app.MapGet("data/getAdminScores", [Authorize] async Task<IResult> (IMemoryCache memoryCache) =>
            {
                List<AdminScoreDto> items = null;
                if (memoryCache.TryGetValue(GetAdminScoresKey, out items))
                    return Results.Ok(items);

                items = GetAdminScores(sqlConnectionStr);
                var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                memoryCache.Set(GetAdminScoresKey, items, cacheOptions);
                return Results.Ok(items);
            });

            app.MapPost("data/importCustomerScore", [Authorize] [DisableRequestSizeLimit]
            async Task<IResult> (IMemoryCache memoryCache, HttpRequest request) =>
            {
                if (!request.Form.Files.Any())
                    return Results.BadRequest("No file found!");

                var formFile = request.Form.Files.FirstOrDefault();

                if (formFile is null || formFile.Length == 0)
                    return Results.BadRequest("No file found!");

                var errorList = new List<CustomerImportErrorDto>();
                var customerRows = new List<string>();
                var customerScoreRows = new List<string>();
                List<AdminScoreDto> adminScores = null;
                if (!memoryCache.TryGetValue(GetAdminScoresKey, out adminScores))
                {
                    adminScores = GetAdminScores(sqlConnectionStr);
                    var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                    memoryCache.Set(GetAdminScoresKey, adminScores, cacheOptions);
                }
                var scoreTiltles = adminScores.Select(p => p.ScoreTitle.ToLower());

                //Process excel file
                using (var stream = new MemoryStream())
                {
                    formFile.CopyTo(stream);
                    using (var connection = new MySqlConnection(sqlConnectionStr))
                    using (ExcelPackage package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                            return Results.BadRequest("No worksheet found!");

                        //read excel file data and add data
                        var isValidPhoneNumber = true;
                        var isValidScoreTiltles = true;
                        var rowCount = worksheet.Dimension.Rows;
                        string dateOccurred;
                        string customerMobileNo;
                        string scoreTitle;
                        string parsedDateOccurredStr;
                        DateTime? parsedDateOccurred;
                        var cells = new List<string>();
                        var errorDetails = new List<string>();
                        
                        for (int row = 2; row <= rowCount; row++)
                        {
                            dateOccurred = (worksheet.Cells[row, 1]?.Value ?? string.Empty).ToString().Trim();
                            customerMobileNo = (worksheet.Cells[row, 2]?.Value ?? string.Empty).ToString().Trim();
                            scoreTitle = (worksheet.Cells[row, 3]?.Value ?? string.Empty).ToString().Trim();
                            parsedDateOccurred = CheckValidDate(dateOccurred);
                            isValidPhoneNumber = CheckValidPhoneNumber(customerMobileNo);
                            isValidScoreTiltles = scoreTiltles.Contains(scoreTitle.ToLower());

                            if (parsedDateOccurred is null)
                            {
                                cells.Add($"Cell A{row}");
                                errorDetails.Add("invalid date");
                            } 

                            if (!isValidPhoneNumber)
                            {
                                cells.Add($"Cell B{row}");
                                errorDetails.Add("invalid mobile No");
                            }

                            if (!isValidScoreTiltles)
                            {
                                cells.Add($"Cell C{row}");
                                errorDetails.Add("invalid score title");
                            }                                

                            if (parsedDateOccurred != null && isValidPhoneNumber && isValidScoreTiltles)
                            {
                                parsedDateOccurredStr = parsedDateOccurred.Value.ToString("yyyy-MM-dd");                                 
                                customerRows.Add(string.Format("('{0}','{1}', {2})", parsedDateOccurredStr, customerMobileNo, 1));
                                customerScoreRows.Add(string.Format("('{0}', {1}, '{2}', {3})",
                                    customerMobileNo,
                                    adminScores.FirstOrDefault(q => q.ScoreTitle.Equals(scoreTitle, StringComparison.OrdinalIgnoreCase))?.ScoreID ?? 0,
                                    parsedDateOccurredStr,
                                    1
                                   ));

                            }
                            else
                            {
                                errorList.Add(new CustomerImportErrorDto
                                {
                                    Cell = string.Join(" - ", cells),
                                    ErrorDetail = string.Join(" - ", errorDetails),
                                    DateOccurred= dateOccurred,
                                    CustomerMobileNo = customerMobileNo,
                                    ScoreTitle = scoreTitle                                    
                                });
                                cells= new List<string>(); //reset after add
                                errorDetails = new List<string>();
                            }
                                
                        }
                    }
                }

                //Parallel.Invoke(
                //    () => { BulkInsertCustomerModelToMySQL(sqlConnectionStr, customerRows); },
                //    () => { BulkInsertCustomerScoreToMySQL(sqlConnectionStr, customerScoreRows); }
                //);

                using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
                {
                    mConnection.Open();                    
                    var sCommand = new StringBuilder("INSERT IGNORE INTO customer (DateFirstAdded, CustomerMobileNo, Status) VALUES ");
                    sCommand.Append(string.Join(",", customerRows)); //may use MySqlHelper.EscapeString
                    sCommand.Append(";");
                    using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }

                    sCommand = new StringBuilder(
                       "INSERT IGNORE INTO customerscore (CustomerMobileNo, ScoreID, DateOccurred, Status) VALUES ");
                    sCommand.Append(string.Join(",", customerScoreRows));
                    sCommand.Append(";");
                    using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }
                }

                return Results.Ok(errorList);
            });            
        }
    }
}