using Microsoft.Extensions.Configuration;

namespace MedcodeETLProcess.Configurations
{
    public class ExcelSettingConfigurations(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public string GetNewCodesFilePath() => _configuration["ExcelFiles:NewCodesFilePath"];

        public string GetOldCodesFilePath() => _configuration["ExcelFiles:OldCodesFilePath"];
    }
}
