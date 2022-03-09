namespace Carrier.Core
{
    public interface ICarrierTransport<TMessageType>
    {
        public Task SendTo<T>(string connectionId, TMessageType messageType, T data);
        public Task SendToAll<T>(TMessageType messageType, T data);
        public Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, T data);
    }
}
