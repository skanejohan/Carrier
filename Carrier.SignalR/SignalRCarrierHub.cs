using Microsoft.AspNetCore.SignalR;

namespace Carrier.SignalR
{
    public class SignalRCarrierHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Core.Carrier.AddReceiver(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Core.Carrier.RemoveReceiver(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
