using MedcodeETLProcess.Helpers;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using Microsoft.Extensions.Logging;

namespace MedcodeETLProcess.Contracts
{
    public abstract class ETLOrchestrator<TInput, TOutput>
    {
        protected readonly IExtractor<TInput> extractor;
        protected readonly ITransformer<TInput, TOutput> transformer;
        protected readonly ILoader<TOutput> loader;
        protected readonly IFilter filter;
        protected readonly ETLConfigurationService configurationService;
        protected readonly IProgressNotifier progressNotifier;
        private readonly ILogger<ETLOrchestrator<TInput, TOutput>> _logger;
        public ETLOrchestrator(IExtractor<TInput> extractor, ITransformer<TInput, TOutput> transformer, ILoader<TOutput> loader, IFilter filter, ETLConfigurationService configurationService, ILogger<ETLOrchestrator<TInput, TOutput>> logger, IProgressNotifier progressNotifier = null)
        {
            this.extractor = extractor;
            this.transformer = transformer;
            this.loader = loader;
            this.filter = filter;
            this.configurationService = configurationService;
            this.progressNotifier = progressNotifier;
            _logger = logger;
        }

        public async Task ExecuteETLProcess(IFilter filter)
        {
            while (true)
            {
                DateTime startExtract = DateTime.Now;
                Console.WriteLine($"Started Extracting Data at:---------------------------- {DateTimeOffset.Now}");
                filter.MaxNumber = filter.MinNumber + filter.BatchSize - 1;
                await ReportProgressAsync(filter, "extract:start", $"Extracting rows {filter.MinNumber}-{filter.MaxNumber}");
                List<TInput> extractedData = extractor.ExtractData(filter);
                DateTime endExtract = DateTime.Now;
                List<ExcelDataMainModel> sp = extractedData as List<ExcelDataMainModel>;

                if (extractedData == null || extractedData.Count == 0)
                {
                    Console.WriteLine("No more data to extract. ETL process completed.");
                    await ReportProgressAsync(filter, "extract:done", "No more data to extract");
                    break;
                }

                if (sp != null && sp.Count > 0)
                {
                    int newCodeCount = sp[0]?.NewCodeDataModels?.Count ?? 0;
                    int oldCodeCount = sp[0]?.OldCodeDataModels?.Count ?? 0;
                    Console.WriteLine($"ExtractionTime(ms): {(endExtract - startExtract).TotalMilliseconds} | Min Number: {filter.MinNumber} | Max Number: {filter.MaxNumber} | New Codes: {newCodeCount} | Old Codes: {oldCodeCount}");
                    await ReportProgressAsync(filter, "extract:batch", $"Extracted batch {filter.MinNumber}-{filter.MaxNumber}", new { newCodes = newCodeCount, oldCodes = oldCodeCount, ms = (endExtract - startExtract).TotalMilliseconds });
                }

                DateTime startTransform = DateTime.Now;
                await ReportProgressAsync(filter, "transform:start", $"Transforming batch {filter.MinNumber}-{filter.MaxNumber}");
                List<TOutput> transformedData = await transformer.TransformData(extractedData, filter);
                DateTime endTransform = DateTime.Now;
                Console.WriteLine($"Transform Time(ms): {(endTransform - startTransform).TotalMilliseconds} | Records: {transformedData.Count}");
                await ReportProgressAsync(filter, "transform:done", $"Transformed batch {filter.MinNumber}-{filter.MaxNumber}", new { records = transformedData.Count, ms = (endTransform - startTransform).TotalMilliseconds });

                DateTime startLoad = DateTime.Now;
                await ReportProgressAsync(filter, "load:start", $"Loading batch {filter.MinNumber}-{filter.MaxNumber}");
                await loader.LoadData(transformedData, filter.CodeVersion);
                DateTime endLoad = DateTime.Now;
                Console.WriteLine($"Load Time(ms): {(endLoad - startLoad).TotalMilliseconds}");
                await ReportProgressAsync(filter, "load:done", $"Loaded batch {filter.MinNumber}-{filter.MaxNumber}", new { ms = (endLoad - startLoad).TotalMilliseconds });

                filter.MinNumber = filter.MaxNumber + 1;
            }
        }

        public abstract Task RunETLProcess();

        protected Task ReportProgressAsync(IFilter filter, string stage, string message, object data = null)
        {
            if (progressNotifier == null || string.IsNullOrWhiteSpace(filter?.RequestId))
            {
                return Task.CompletedTask;
            }
            return progressNotifier.ReportAsync(filter.RequestId, stage, message, data);
        }
    }
}
