namespace Carrier.Core
{
    public interface ICarrier<TMessageType>
    {
        // Messages to clients
        public Task SendTo<T>(string connectionId, TMessageType messageType, T data);
        public Task SendToAll<T>(TMessageType messageType, T data);
        public Task SendToAll<T>(TMessageType messageType, Func<string, T> getData);
        public Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, T data);
        public Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData);

        public Task<bool> SendToAndAwaitAck<T>(string connectionId, TMessageType messageType, T data, int maxMs = 5000);
        public Task<bool> SendToAllAndAwaitAck<T>(TMessageType messageType, T data, int maxMs = 5000);
        public Task<bool> SendToAllAndAwaitAck<T>(TMessageType messageType, Func<string, T> getData, int maxMs = 5000);
        public Task<bool> SendToAllExceptAndAwaitAck<T>(string exceptConnectionId, TMessageType messageType, T data, int maxMs = 5000);
        public Task<bool> SendToAllExceptAndAwaitAck<T>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData, int maxMs = 5000);

        public Task<(bool, T2?)> SendToAndAwaitAnswer<T, T2>(string connectionId, TMessageType messageType, T data, int maxMs = 5000);
        public Task<Dictionary<string, T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, T data, int maxMs = 5000);
        public Task<IEnumerable<T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, Func<string, T> getData, int maxMs = 5000);
        public Task<IEnumerable<T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, T data, int maxMs = 5000);
        public Task<IEnumerable<T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData, int maxMs = 5000);

        // Responses from clients
        public void Ack(string connectionId);
        public void Answer(string connectionId, string json);
    }
}
