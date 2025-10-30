using System.Threading.Tasks;

namespace MedcodeETLProcess.Contracts
{
    public interface IProgressNotifier
    {
        Task ReportAsync(string requestId, string stage, string message, object data = null);
    }
}
