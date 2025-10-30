using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Helpers;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using MedcodeETLProcess.Model.MedcodeModel;
using Microsoft.Extensions.Logging;

namespace MedcodeETLProcess.Implementation
{
    public class ExcelToSQLServerETLOrchestrator(
        IExtractor<ExcelDataMainModel> extractor,
        ITransformer<ExcelDataMainModel, MedcodeDataMainModel> transformer,
        ILoader<MedcodeDataMainModel> loader,
        IFilter filter,
        ETLConfigurationService eTLConfigurationService,
        ILogger<ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel>> logger,
        IExcelDataRepository<ExcelDataMainModel> excelRepository,
        IProgressNotifier progressNotifier) : ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel>(extractor, transformer, loader, filter, eTLConfigurationService, logger, progressNotifier)
    {
        private readonly IExcelDataRepository<ExcelDataMainModel> _excelRepository = excelRepository;

        public override async Task RunETLProcess()
        {
            Console.WriteLine($"==========================================================================");
            Console.WriteLine($"ETL process started at: {DateTimeOffset.Now}");
            Console.WriteLine($"==========================================================================");
            Console.WriteLine($"\n[PHASE 1] Starting NEW CODES processing...");
            filter.ProcessingPhase = "NEW_CODES";
            filter.ProcessedNewCodes.Clear();
            await ReportProgressAsync(filter, "phase:new:start", "Starting NEW CODES processing");

            int totalNewCodeRows = _excelRepository.GetTotalNewCodeRows(filter);
            Console.WriteLine($"Total new code rows to process: {totalNewCodeRows}");
            await ReportProgressAsync(filter, "phase:new:total", $"Total new code rows: {totalNewCodeRows}", new { total = totalNewCodeRows });

            if (totalNewCodeRows > 0)
            {
                await ExecuteETLProcess(filter);
                Console.WriteLine($"[PHASE 1] NEW CODES processing completed. Total codes processed: {filter.ProcessedNewCodes.Count}");
                await ReportProgressAsync(filter, "phase:new:done", "NEW CODES processing completed", new { processedNewCodes = filter.ProcessedNewCodes.Count });
            }
            else
            {
                Console.WriteLine($"[PHASE 1] No new codes to process.");
                await ReportProgressAsync(filter, "phase:new:done", "No new codes to process");
            }
            Console.WriteLine($"\n[PHASE 2] Starting OLD CODES processing...");
            filter.ProcessingPhase = "OLD_CODES";
            filter.MinNumber = 1;
            await ReportProgressAsync(filter, "phase:old:start", "Starting OLD CODES processing");

            int totalOldCodeRows = _excelRepository.GetTotalOldCodeRows(filter);
            Console.WriteLine($"Total old code rows to process: {totalOldCodeRows}");
            Console.WriteLine($"Will only update codes that exist in the {filter.ProcessedNewCodes.Count} new codes just processed.");
            await ReportProgressAsync(filter, "phase:old:total", $"Total old code rows: {totalOldCodeRows}", new { total = totalOldCodeRows, restrictToNewCodes = filter.ProcessedNewCodes.Count });

            if (totalOldCodeRows > 0)
            {
                await ExecuteETLProcess(filter);
                Console.WriteLine($"[PHASE 2] OLD CODES processing completed.");
                await ReportProgressAsync(filter, "phase:old:done", "OLD CODES processing completed");
            }
            else
            {
                Console.WriteLine($"[PHASE 2] No old codes to process.");
                await ReportProgressAsync(filter, "phase:old:done", "No old codes to process");
            }

            Console.WriteLine($"\n==========================================================================");
            Console.WriteLine($"ETL process completed at: {DateTimeOffset.Now}");
            Console.WriteLine($"==========================================================================");
            await ReportProgressAsync(filter, "etl:done", "ETL process completed");
        }
    }
}
