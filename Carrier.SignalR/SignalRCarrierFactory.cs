using Carrier.Core;
using Microsoft.AspNetCore.SignalR;

namespace Carrier.SignalR
{
    public static class SignalRCarrierFactory
    {
        public static bool TryGetCarrier<TMessageType>(IServiceProvider serviceProvider, out ICarrier<TMessageType>? carrier)
        {
            var context = serviceProvider.GetService(typeof(IHubContext<SignalRCarrierHub>));

            if (context == null)
            {
                carrier = null;
                return false;
            }

            carrier = new Carrier<TMessageType>(new SignalRCarrierTransport<TMessageType>((IHubContext<SignalRCarrierHub>)context));
            return true;
        }
    }
}
