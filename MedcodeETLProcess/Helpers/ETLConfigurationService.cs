using Microsoft.Extensions.Configuration;

namespace MedcodeETLProcess.Helpers
{
    public class ETLConfigurationService(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public int GetBatchSize() => _configuration.GetValue<int>("ETLConfiguration:BatchSize");
    }
}
