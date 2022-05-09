using BITool.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
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

        private static DateTime? CheckValidDate(string input)
        {
            DateTime result;
            var checkParse = true;
            checkParse = DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd/MM/yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MM-yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MM-yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MMM-yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParseExact(input, "dd-MMM-yyyy hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result);

            if (!checkParse)
                checkParse = DateTime.TryParse(input, out result);

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

        private static void BulkInsertCustomerModelToMySQL(string sqlConnectionStr, IEnumerable<string> customerMobileList)
        {
            var nowStr = DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss");
            //add INSERT IGNORE to avoid throw error when duplicate CustomerMobileNo
            var sCommand = new StringBuilder("INSERT IGNORE INTO Customer (DateFirstAdded, CustomerMobileNo, Status) VALUES ");
            using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
            {
                List<string> Rows = new List<string>();
                foreach (var phone in customerMobileList)
                {
                    Rows.Add(string.Format("('{0}','{1}', {2})",
                        MySqlHelper.EscapeString(nowStr),
                        MySqlHelper.EscapeString(phone),
                        1
                        ));
                }

                sCommand.Append(string.Join(",", Rows));
                sCommand.Append(";");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }

        private static void BulkInsertCustomerScoreToMySQL(string sqlConnectionStr, IEnumerable<CustomerScoreDto> items)
        {
            var sCommand = new StringBuilder(
                "INSERT IGNORE INTO CustomerScore (CustomerMobileNo, ScoreID, DateOccurred, Status) VALUES ");
            using (MySqlConnection mConnection = new MySqlConnection(sqlConnectionStr))
            {
                List<string> Rows = new List<string>();
                foreach (var item in items)
                {
                    Rows.Add(string.Format("('{0}', {1}, '{2}', {3})",
                        item.CustomerMobileNo,
                        item.ScoreID,
                        item.DateOccurred,
                        item.Status
                       ));
                }

                sCommand.Append(string.Join(",", Rows));
                sCommand.Append(";");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
        }

        #endregion Private

        public static void AddImportDataService(this WebApplication app)
        {
            app.MapGet("data/getAdminScores", [Authorize] async Task<IResult> (IConfiguration configuration) =>
            {
                var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];
                using (var connection = new MySqlConnection(sqlConnectionStr))
                {
                    using (var conn = new MySqlConnection(sqlConnectionStr))
                    {
                        var adminScores = conn.Query<AdminScoreDto>("SELECT * FROM adminscore")
                                                         .ToList();
                        return Results.Ok(adminScores);
                    }
                }
            });

            app.MapPost("data/importCustomerScore", [AllowAnonymous] async Task<IResult> (IConfiguration configuration, HttpRequest request) =>
            {
                if (!request.Form.Files.Any())
                    return Results.BadRequest("No file found!");

                var formFile = request.Form.Files.FirstOrDefault();

                if (formFile is null || formFile.Length == 0)
                    return Results.BadRequest("No file found!");

                var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];
                var adminScores = new List<AdminScoreDto>();
                var customerScoreList = new List<CustomerImportDto>();
                var errorList = new List<string>();
                using (var conn = new MySqlConnection(sqlConnectionStr))
                {
                    adminScores = conn.Query<AdminScoreDto>("SELECT * FROM adminscore")
                                                    .ToList();
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
                            return Results.BadRequest();

                        //read excel file data and add data
                        var isValidPhoneNumber = true;
                        var isValidScoreTiltles = true;
                        var rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var item = new CustomerImportDto
                            {
                                DateOccurred = (worksheet.Cells[row, 1].Value ?? string.Empty).ToString().Trim(),
                                CustomerMobileNo = (worksheet.Cells[row, 2].Value ?? string.Empty).ToString().Trim(),
                                ScoreTitle = (worksheet.Cells[row, 3].Value ?? string.Empty).ToString().Trim().ToLower(),
                            };
                            item.ParsedDateOccurred = CheckValidDate(item.DateOccurred);
                            if (item.ParsedDateOccurred is null)
                                errorList.Add($"Cell A{row} - date invalid");

                            isValidPhoneNumber = CheckValidPhoneNumber(item.CustomerMobileNo);
                            if (!isValidPhoneNumber)
                                errorList.Add($"Cell B{row} - mobile invalid");

                            isValidScoreTiltles = scoreTiltles.Contains(item.ScoreTitle);
                            if (!isValidScoreTiltles)
                                errorList.Add($"Cell C{row} - score title invalid");

                            if (item.ParsedDateOccurred != null && isValidPhoneNumber && isValidScoreTiltles)
                                customerScoreList.Add(item);
                        }
                    }
                }

                //Insert Customer
                var customerMobileList = customerScoreList.Select(p => p.CustomerMobileNo).Distinct();
                //BulkInsertCustomerModelToMySQL(sqlConnectionStr, customerMobileList);

                //Insert CustomerScore
                var customerScores = new List<CustomerScoreDto>();
                customerScores = customerScoreList.Select(p => new CustomerScoreDto
                {
                    CustomerMobileNo = p.CustomerMobileNo,
                    ScoreID = adminScores.FirstOrDefault(q => q.ScoreTitle.Equals(p.ScoreTitle,StringComparison.OrdinalIgnoreCase))?.ScoreID ?? 0,
                    DateOccurred = p.DateOccurred, //
                    Status = 1
                }).ToList();
                //BulkInsertCustomerScoreToMySQL(sqlConnectionStr, customerScores);
                Parallel.Invoke(
                    () =>{ BulkInsertCustomerModelToMySQL(sqlConnectionStr, customerMobileList); },
                    () =>{ BulkInsertCustomerScoreToMySQL(sqlConnectionStr, customerScores); }
                );

                return Results.Ok(errorList);
            })/*.Accepts<IFormFile>("multipart/form-data")*/;

            
            app.MapPost("data/importCustomerScoreList", [AllowAnonymous] async Task<IResult> (IConfiguration configuration, ImportCustomerScore input) =>
            {
                if(input == null ||input.CustomerList.Count==0)
                    return Results.BadRequest("No file data found!");

                var sqlConnectionStr = configuration["ConnectionStrings:DefaultConnection"];
                var adminScores = new List<AdminScoreDto>();
                using (var conn = new MySqlConnection(sqlConnectionStr))
                {
                    adminScores = conn.Query<AdminScoreDto>("SELECT * FROM adminscore")
                                                    .ToList();
                }
                var scoreTiltles = adminScores.Select(p => p.ScoreTitle.ToLower());
                
                //Insert Customer
                var customerMobileList = input.CustomerList.Select(p => p.CustomerMobileNo).Distinct();
                //BulkInsertCustomerModelToMySQL(sqlConnectionStr, customerMobileList);

                //Insert CustomerScore
                var customerScores = new List<CustomerScoreDto>();
                customerScores = input.CustomerList.Select(p => new CustomerScoreDto
                {
                    CustomerMobileNo = p.CustomerMobileNo,
                    ScoreID = adminScores.FirstOrDefault(q => q.ScoreTitle.Equals(p.ScoreTitle,StringComparison.OrdinalIgnoreCase))?.ScoreID ?? 0,
                    DateOccurred = p.DateOccurred, //
                    Status = 1
                }).ToList();
                //BulkInsertCustomerScoreToMySQL(sqlConnectionStr, customerScores);
                Parallel.Invoke(
                    () => { BulkInsertCustomerModelToMySQL(sqlConnectionStr, customerMobileList); },
                    () => { BulkInsertCustomerScoreToMySQL(sqlConnectionStr, customerScores); }
                );

                return Results.Ok();
            });
        }
    }
}