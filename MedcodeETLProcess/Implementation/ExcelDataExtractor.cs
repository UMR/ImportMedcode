using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.ExcelDataMainModel;

namespace MedcodeETLProcess.Implementation
{
    public class ExcelDataExtractor(IExcelDataRepository<ExcelDataMainModel> repository) : IExtractor<ExcelDataMainModel>
    {
        private readonly IExcelDataRepository<ExcelDataMainModel> _repository = repository;

        public List<ExcelDataMainModel> ExtractData(IFilter filter)
        {
            try
            {
                var excelDataList = new List<ExcelDataMainModel>();

                if (filter.ProcessingPhase == "NEW_CODES")
                {
                    var newCodeData = _repository.ExtractNewCodeData(filter);

                    if (newCodeData.Count > 0)
                    {
                        var excelData = new ExcelDataMainModel
                        {
                            NewCodeDataModels = newCodeData,
                            OldCodeDataModels = new List<OldCodeDataUpdateModel>(),
                            ProcessingPhase = "NEW_CODES"
                        };
                        excelDataList.Add(excelData);
                    }
                }
                else if (filter.ProcessingPhase == "OLD_CODES")
                {
                    var oldCodeData = _repository.ExtractOldCodeData(filter);

                    if (oldCodeData.Count > 0)
                    {
                        var excelData = new ExcelDataMainModel
                        {
                            NewCodeDataModels = new List<NewCodeDataModel>(),
                            OldCodeDataModels = oldCodeData,
                            ProcessingPhase = "OLD_CODES"
                        };
                        excelDataList.Add(excelData);
                    }
                }

                return excelDataList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting data: {ex.Message}");
                throw;
            }
        }
    }
}
