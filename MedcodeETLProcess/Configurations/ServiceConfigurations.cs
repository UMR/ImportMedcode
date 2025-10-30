using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Helpers;
using MedcodeETLProcess.Implementation;
using MedcodeETLProcess.Model.ExcelDataMainModel;
using MedcodeETLProcess.Model.MedcodeModel;
using MedcodeETLProcess.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedcodeETLProcess.Configurations
{
    public static class ServiceConfigurations
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ETLConfigurationService>();
            services.AddSingleton<ExcelSettingConfigurations>();
            services.AddSingleton<DapperDbContext>();
            services.AddScoped<IFilter, Filter>();
            services.AddScoped<IExcelDataRepository<ExcelDataMainModel>, ExcelDataRepository>();
            services.AddScoped<IMedcodeRepository<MedcodeDataMainModel>, MedcodeDataRepository>();
            services.AddScoped<IExtractor<ExcelDataMainModel>, ExcelDataExtractor>();
            services.AddScoped<ITransformer<ExcelDataMainModel, MedcodeDataMainModel>>(provider =>
            {
                var repository = provider.GetRequiredService<IMedcodeRepository<MedcodeDataMainModel>>();
                return new ExcelToSQLServerDataTransformer(repository, provider.GetRequiredService<IProgressNotifier>());
            });
            services.AddScoped<ILoader<MedcodeDataMainModel>, Loader>();
            services.AddScoped<ETLOrchestrator<ExcelDataMainModel, MedcodeDataMainModel>, ExcelToSQLServerETLOrchestrator>();
        }
    }
}
