using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Carrier.SignalR
{
    public static class SignalRCarrierExtensions
    {
        public static ISignalRServerBuilder AddSignalRCarrier(this IServiceCollection services)
        {
            return services.AddSignalR();
        }

        public static HubEndpointConventionBuilder MapCarrierHub(this IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapHub<SignalRCarrierHub>("/carrierhub");
        }

        public static IHubContext<SignalRCarrierHub> GetCarrierHubContext<T>(this IServiceProvider services)
        {
            return (IHubContext<SignalRCarrierHub>)services.GetService(typeof(IHubContext<SignalRCarrierHub>));
        }
    }
}
