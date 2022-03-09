using Carrier.Core;

namespace Carrier.SignalR
{
    public static class SignalRCarrierFactory
    {
        public static ICarrier<TMessageType> GetCarrier<TMessageType>(IServiceProvider serviceProvider)
        {
            return new Carrier<TMessageType>(new SignalRCarrierTransport<TMessageType>(serviceProvider.GetCarrierHubContext<TMessageType>()));
        }
    }
}
