using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using OfficeOpenXml;

namespace MedcodeETLProcess.Repository
{
    public class ExcelDataRepository : IExcelDataRepository<ExcelDataMainModel>
    {
        public ExcelDataRepository()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<NewCodeDataModel> ExtractNewCodeData(IFilter filter)
        {
            var newCodeList = new List<NewCodeDataModel>();

            try
            {
                if (filter.MedcodeNewFile == null || filter.MedcodeNewFile.Length == 0)
                {
                    Console.WriteLine("New code Excel file is not provided.");
                    return newCodeList;
                }

                using (var stream = new MemoryStream(filter.MedcodeNewFile))
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount == 0)
                    {
                        return newCodeList;
                    }
                    int startRow = filter.MinNumber + 1;
                    int endRow = Math.Min(filter.MaxNumber + 1, rowCount);

                    for (int row = startRow; row <= endRow; row++)
                    {
                        var code = worksheet.Cells[row, 2].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(code))
                            continue;

                        var newCodeData = new NewCodeDataModel
                        {
                            Order_Number = worksheet.Cells[row, 1].Value?.ToString(),
                            Code = code,
                            Level = worksheet.Cells[row, 3].Value?.ToString(),
                            Short_Description = worksheet.Cells[row, 4].Value?.ToString(),
                            Long_Description = worksheet.Cells[row, 5].Value?.ToString()
                        };

                        newCodeList.Add(newCodeData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting new code data: {ex.Message}");
                throw;
            }

            return newCodeList;
        }

        public List<OldCodeDataUpdateModel> ExtractOldCodeData(IFilter filter)
        {
            var oldCodeList = new List<OldCodeDataUpdateModel>();

            try
            {
                if (filter.MedcodeOldFile == null || filter.MedcodeOldFile.Length == 0)
                {
                    Console.WriteLine("Old code Excel file is not provided.");
                    return oldCodeList;
                }

                using (var stream = new MemoryStream(filter.MedcodeOldFile))
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount == 0)
                    {
                        return oldCodeList;
                    }
                    int startRow = filter.MinNumber + 1;
                    int endRow = Math.Min(filter.MaxNumber + 1, rowCount);

                    for (int row = startRow; row <= endRow; row++)
                    {
                        var code = worksheet.Cells[row, 2].Value?.ToString();

                        if (string.IsNullOrWhiteSpace(code))
                            continue;

                        var oldCodeData = new OldCodeDataUpdateModel
                        {
                            Status = worksheet.Cells[row, 1].Value?.ToString(),
                            Code = code,
                            Description = worksheet.Cells[row, 3].Value?.ToString()
                        };

                        oldCodeList.Add(oldCodeData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting old code data: {ex.Message}");
                throw;
            }

            return oldCodeList;
        }

        public int GetTotalNewCodeRows(IFilter filter)
        {
            try
            {
                if (filter.MedcodeNewFile == null || filter.MedcodeNewFile.Length == 0)
                {
                    return 0;
                }

                using (var stream = new MemoryStream(filter.MedcodeNewFile))
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    return rowCount > 1 ? rowCount - 1 : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting total new code rows: {ex.Message}");
                return 0;
            }
        }

        public int GetTotalOldCodeRows(IFilter filter)
        {
            try
            {
                if (filter.MedcodeOldFile == null || filter.MedcodeOldFile.Length == 0)
                {
                    return 0;
                }

                using (var stream = new MemoryStream(filter.MedcodeOldFile))
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    return rowCount > 1 ? rowCount - 1 : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting total old code rows: {ex.Message}");
                return 0;
            }
        }

    }
}
