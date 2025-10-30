using Microsoft.Extensions.DependencyInjection;

namespace MedcodeETLProcess.Helpers
{
    public class ServiceFactory
    {
        private static ServiceProvider serviceProvider;
        public ServiceFactory(ServiceProvider services)
        {
            serviceProvider = services;
        }
        public static ServiceProvider GetProvider()
        {
            return serviceProvider;
        }
    }
}
