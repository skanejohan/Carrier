using Carrier.Core;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Carrier.SignalR
{
    public class SignalRCarrierTransport<TMessageType> : ICarrierTransport<TMessageType>
    {
        public SignalRCarrierTransport(IHubContext<SignalRCarrierHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public Task SendTo<T>(string connectionId, TMessageType messageType, T data)
        {
            return hubContext.Clients.Client(connectionId).SendAsync(ToStringSafe(messageType), JsonSerializer.Serialize(data));
        }

        public Task SendToAll<T>(TMessageType messageType, T data)
        {
            return hubContext.Clients.All.SendAsync(ToStringSafe(messageType), JsonSerializer.Serialize(data));
        }

        public Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, T data)
        {
            return hubContext.Clients.AllExcept(exceptConnectionId).SendAsync(ToStringSafe(messageType), JsonSerializer.Serialize(data));
        }

        private static string ToStringSafe(TMessageType messageType) 
            => messageType?.ToString() ?? "null";

        private readonly IHubContext<SignalRCarrierHub> hubContext;
    }
}
