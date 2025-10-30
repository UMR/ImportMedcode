using System.Threading.Tasks;
using MedcodeETLProcess.Contracts;
using Microsoft.AspNetCore.SignalR;
using Medcode.Presentation.Hubs;

namespace Medcode.Presentation.Notifications
{
    public class SignalRProgressNotifier : IProgressNotifier
    {
        private readonly IHubContext<ETLHub> _hubContext;

        public SignalRProgressNotifier(IHubContext<ETLHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task ReportAsync(string requestId, string stage, string message, object data = null)
        {
            var payload = new
            {
                requestId,
                stage,
                message,
                data,
                timestamp = System.DateTimeOffset.UtcNow
            };
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return _hubContext.Clients.All.SendAsync("progress", payload);
            }
            return _hubContext.Clients.Group(requestId).SendAsync("progress", payload);
        }
    }
}
