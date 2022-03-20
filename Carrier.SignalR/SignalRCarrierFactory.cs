using Carrier.Core;
using Microsoft.AspNetCore.SignalR;

namespace Carrier.SignalR
{
    public static class SignalRCarrierFactory
    {
        public static bool TryGetCarrier<TMessageType>(IServiceProvider serviceProvider, out ICarrier<TMessageType>? carrier, 
            out ICarrierMonitor? carrierMonitor)
        {
            var context = serviceProvider.GetService(typeof(IHubContext<SignalRCarrierHub>));

            if (context == null)
            {
                carrier = null;
                carrierMonitor = null;
                return false;
            }

            carrier = Carrier<TMessageType>.Create(new SignalRCarrierTransport<TMessageType>((IHubContext<SignalRCarrierHub>)context));
            carrierMonitor = carrier as ICarrierMonitor;
            return true;
        }
    }
}
