using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using MedcodeETLProcess.Model.MedcodeModel;

namespace MedcodeETLProcess.Implementation
{
    public class ExcelToSQLServerDataTransformer(IMedcodeRepository<MedcodeDataMainModel> repository, IProgressNotifier progressNotifier) : ITransformer<ExcelDataMainModel, MedcodeDataMainModel>
    {
        private readonly IMedcodeRepository<MedcodeDataMainModel> _repository = repository;
        private readonly IProgressNotifier _progressNotifier = progressNotifier;

        public async Task<List<MedcodeDataMainModel>> TransformData(List<ExcelDataMainModel> rawData, IFilter filter)
        {
            var transformedDataList = new List<MedcodeDataMainModel>();

            try
            {
                var allOldCodes = rawData
                    .Where(e => e.OldCodeDataModels != null)
                    .SelectMany(e => e.OldCodeDataModels)
                    .Select(o => o.Code)
                    .Distinct()
                    .ToList();

                HashSet<string> existingCodesInDB = new HashSet<string>();
                if (allOldCodes.Any())
                {
                    existingCodesInDB = await _repository.GetExistingCodes(allOldCodes);
                    Console.WriteLine($"[BULK CHECK] Found {existingCodesInDB.Count} existing codes out of {allOldCodes.Count} old codes");
                    await _progressNotifier.ReportAsync(filter.RequestId, "transform:old:existingcodes", $"Found {existingCodesInDB.Count} existing codes out of {allOldCodes.Count} old codes", new { existingCodes = existingCodesInDB.Count, totalOldCodes = allOldCodes.Count });
                }

                foreach (var excelData in rawData)
                {
                    if (excelData.NewCodeDataModels != null && excelData.NewCodeDataModels.Any())
                    {
                        foreach (var newCode in excelData.NewCodeDataModels)
                        {
                            var medcodeData = TransformNewCodeToMedcode(newCode, filter);
                            transformedDataList.Add(medcodeData);
                            filter.ProcessedNewCodes.Add(newCode.Code);
                        }
                    }

                    if (excelData.OldCodeDataModels != null && excelData.OldCodeDataModels.Any())
                    {
                        foreach (var oldCode in excelData.OldCodeDataModels)
                        {
                            if (existingCodesInDB.Contains(oldCode.Code))
                            {
                                var medcodeData = TransformOldCodeToMedcode(oldCode, filter);
                                transformedDataList.Add(medcodeData);
                            }
                            else
                            {
                                Console.WriteLine($"[SKIP] Code: {oldCode.Code} | Status: {oldCode.Status} | Reason: Not found in database");
                                await _progressNotifier.ReportAsync(filter.RequestId, "transform:old:skip", $"Skipping code {oldCode.Code} as it does not exist in database", new { code = oldCode.Code, status = oldCode.Status });
                            }
                        }
                    }
                }

                return await Task.FromResult(transformedDataList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transforming data: {ex.Message}");
                throw;
            }
        }

        private MedcodeDataMainModel TransformNewCodeToMedcode(NewCodeDataModel newCode, IFilter filter)
        {
            var guid = Guid.NewGuid();
            var medcodeData = new MedcodeDataMainModel
            {
                MedicalCode = new MedicalCode
                {
                    Guid = guid,
                    MedCode = newCode.Code,
                    Detail = newCode.Long_Description,
                    MedCodeValue = null,
                    MedCodeStatus = "N",
                    MedicalContentIndex = newCode.Short_Description,
                    CodeType = filter.CodeType,
                    IsCodingCompleted = newCode.Level == "0",
                    IsRecycledEver = false,
                    NoMetric = false
                },
                MedicalCodesAction = new MedicalCodesAction
                {
                    UserId = "BDAdmin",
                    Date = DateTime.Now,
                    Guid = guid,
                    Action = "N",
                },
                MedicalCodeHistory = new MedicalCodeHistory
                {
                    UserId = "BDAdmin",
                    Date = DateTime.Now,
                    Action = "New",
                    MedCode = newCode.Code,
                    CodeType = filter.CodeType,
                    IsCodingCompleted = newCode.Level == "0"
                },
                MedicalCodeActionHistory = new MedcodeActionHistory
                {
                    CodeType = filter.CodeType,
                    MedCode = newCode.Code,
                    CodeVersion = !string.IsNullOrEmpty(filter.CodeVersion) ? Convert.ToInt32(filter.CodeVersion) : 2012,
                    Action = "N",
                    Time = DateTime.Now
                },
                MedcodepediaHistoryDBMedcode = new MedcodepediaHistoryDBMedcode
                {
                    CodeType = filter.CodeType,
                    MedCode = newCode.Code,
                    Detail = newCode.Long_Description,
                    MedicalContentIndex = newCode.Short_Description,
                    MedCodeValue = null,
                    MedCodeStatus = "N",
                    IsCodingCompleted = newCode.Level == "0",
                    SexType = "B",
                    CodeVersion = filter.CodeVersion
                }
            };

            return medcodeData;
        }

        private MedcodeDataMainModel TransformOldCodeToMedcode(OldCodeDataUpdateModel oldCode, IFilter filter)
        {
            var medcodeData = new MedcodeDataMainModel
            {
                MedicalCode = new MedicalCode
                {
                    Guid = Guid.Empty,
                    MedCode = oldCode.Code,
                    Detail = oldCode.Description,
                    MedCodeStatus = oldCode.Status
                },
                MedicalCodesAction = new MedicalCodesAction
                {
                    UserId = "BDAdmin",
                    Date = DateTime.Now,
                    Action = oldCode.Status
                },
                MedicalCodeHistory = new MedicalCodeHistory
                {
                    UserId = "BDAdmin",
                    Date = DateTime.Now,
                    Action = GetActionDescription(oldCode.Status),
                    MedCode = oldCode.Code,
                    CodeType = filter.CodeType,
                    UmedSysStat = GetStatusDescription(oldCode.Status),
                    IsCodingCompleted = true
                },
                MedicalCodeActionHistory = new MedcodeActionHistory
                {
                    CodeType = filter.CodeType,
                    MedCode = oldCode.Code,
                    Action = oldCode.Status,
                    Time = DateTime.Now
                },
                MedcodepediaHistoryDBMedcode = new MedcodepediaHistoryDBMedcode
                {
                    CodeType = filter.CodeType,
                    MedCode = oldCode.Code,
                    Detail = oldCode.Description,
                    MedCodeStatus = oldCode.Status,
                    IsCodingCompleted = true,
                    SexType = "B",
                    CodeVersion = filter.CodeVersion
                }
            };

            return medcodeData;
        }

        private string GetActionDescription(string status) => status?.ToUpper() switch
        {
            "D" => "Delete",
            "A" => "Active",
            "O" => "Old",
            "N" => "New",
            _ => "Edit"
        };

        private string GetStatusDescription(string status) => status?.ToUpper() switch
        {
            "D" => "Deleted",
            "A" => "Active",
            "O" => "Obsolete",
            "N" => "New",
            _ => status
        };
    }
}
