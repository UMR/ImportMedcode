using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using MedcodeETLProcess.Model.MedcodeModel;
using Microsoft.Extensions.Logging;

namespace MedcodeETLProcess.Services
{
    public class ETLService(
        ILogger<ETLService> logger,
        ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel> etlOrchestrator,
        IFilter filter)
    {
        private readonly ILogger<ETLService> _logger = logger;
        private readonly ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel> _etlOrchestrator = etlOrchestrator;
        private readonly IFilter _filter = filter;

        public async Task<ETLResult> ExecuteETLAsync(ETLRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting ETL process for CodeType: {request.CodeType}, CodeVersion: {request.CodeVersion}");

                _filter.CodeType = request.CodeType;
                _filter.CodeVersion = request.CodeVersion;
                _filter.MinNumber = request.MinNumber ?? 1;
                _filter.BatchSize = request.BatchSize ?? 1000;
                _filter.MedcodeNewFile = request.MedcodeNewFile;
                _filter.MedcodeOldFile = request.MedcodeOldFile;
                _filter.ProcessedNewCodes = new HashSet<string>();
                _filter.RequestId = string.IsNullOrWhiteSpace(request.RequestId) ? Guid.NewGuid().ToString("N") : request.RequestId;

                if (_filter.MedcodeNewFile == null || _filter.MedcodeNewFile.Length == 0)
                {
                    return new ETLResult
                    {
                        Success = false,
                        Message = "New codes file is required"
                    };
                }

                _logger.LogInformation($"New codes file size: {_filter.MedcodeNewFile.Length} bytes");
                _logger.LogInformation($"Old codes file size: {_filter.MedcodeOldFile?.Length ?? 0} bytes");

                await _etlOrchestrator.RunETLProcess();

                _logger.LogInformation("ETL process completed successfully");

                return new ETLResult
                {
                    Success = true,
                    Message = "ETL process completed successfully",
                    ProcessedNewCodes = _filter.ProcessedNewCodes.Count,
                    ProcessedOldCodes = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"ETL process failed: {ex.Message}", ex);
                return new ETLResult
                {
                    Success = false,
                    Message = $"ETL process failed: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        public string StartBackgroundETL(ETLRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                request.RequestId = Guid.NewGuid().ToString("N");
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteETLAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background ETL failed for RequestId {RequestId}", request.RequestId);
                }
            });

            return request.RequestId;
        }
    }

    public class ETLRequest
    {
        public string CodeType { get; set; }
        public string CodeVersion { get; set; }
        public int? MinNumber { get; set; }
        public int? BatchSize { get; set; }
        public byte[] MedcodeNewFile { get; set; }
        public byte[] MedcodeOldFile { get; set; }
        public string RequestId { get; set; }
    }

    public class ETLResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ProcessedNewCodes { get; set; }
        public int ProcessedOldCodes { get; set; }
        public string ErrorDetails { get; set; }
    }
}
