using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Helpers;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using MedcodeETLProcess.Model.MedcodeModel;
using MedcodeETLProcess.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace MedcodeETLProcess
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ETLConfigurationService _etlConfig;
        private readonly ExcelSettingConfigurations _excelConfig;
        private readonly IFilter _filter;

        public Worker(ILogger<Worker> logger, ETLConfigurationService etlConfig, ExcelSettingConfigurations excelConfig, IFilter filter)
        {
            _logger = logger;
            _etlConfig = etlConfig;
            _excelConfig = excelConfig;
            _filter = filter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service starting...");
            await ExecuteETLProcess();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service stopping...");
            return Task.CompletedTask;
        }

        public async Task ExecuteETLProcess()
        {
            try
            {
                string newCodesPath = _excelConfig.GetNewCodesFilePath();
                string oldCodesPath = _excelConfig.GetOldCodesFilePath();
                _filter.CodeType = "ICD-10-CM";
                _filter.MedcodeNewFile = LoadExcelFile(newCodesPath);
                _filter.MedcodeOldFile = LoadExcelFile(oldCodesPath);
                if (_filter.MedcodeNewFile.Length == 0)
                {
                    _logger.LogError("New codes Excel file is required but not loaded. Please check file path.");
                    return;
                }

                _logger.LogInformation($"New codes file loaded: {_filter.MedcodeNewFile.Length} bytes");
                _logger.LogInformation($"Old codes file loaded: {_filter.MedcodeOldFile.Length} bytes");
                var etlProcessManager = ServiceFactory.GetProvider()
                    .GetRequiredService<ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel>>();

                await etlProcessManager.RunETLProcess();

                _logger.LogInformation("ETL Process completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute ETL process: {ex}");
                throw;
            }
        }

        private byte[] LoadExcelFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogWarning("File path is empty or null");
                    return [];
                }

                string projectRoot = AppContext.BaseDirectory;
                if (projectRoot.Contains("bin"))
                {
                    projectRoot = Directory.GetParent(projectRoot).Parent.Parent.Parent.FullName;
                }
                string fullPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : Path.Combine(projectRoot, filePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (File.Exists(fullPath))
                {
                    _logger.LogInformation($"Loading Excel file: {fullPath}");
                    return File.ReadAllBytes(fullPath);
                }
                else
                {
                    _logger.LogWarning($"File not found: {fullPath}");
                    return [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading Excel file '{filePath}': {ex.Message}");
                return [];
            }
        }
    }
}
