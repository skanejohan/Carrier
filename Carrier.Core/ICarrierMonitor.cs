namespace Carrier.Core
{
    public interface ICarrierMonitor
    {
        public Action<IEnumerable<string>>? OnClientsUpdated { get; set; }
    }
}
