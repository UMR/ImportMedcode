using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Medcode.Presentation.Hubs
{
    public class ETLHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public async Task JoinRequest(string requestId)
        {
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, requestId);
            }
        }
    }
}
