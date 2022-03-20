using Microsoft.AspNetCore.SignalR;

namespace Carrier.SignalR
{
    public class SignalRCarrierHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Core.Carrier.Instance?.AddReceiver(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Core.Carrier.Instance?.RemoveReceiver(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
