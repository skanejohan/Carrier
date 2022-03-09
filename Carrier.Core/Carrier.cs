using System.Collections.Concurrent;

namespace Carrier.Core
{
    public class Carrier
    {
        public static void AddReceiver(string id)
        {
            receivers[id] = new Receiver();
        }

        public static void RemoveReceiver(string id)
        {
            receivers.TryRemove(id, out var _);
        }

        protected static Receiver GetReceiver(string id)
        {
            return receivers[id] ?? dummyReceiver;
        }

        protected static IEnumerable<string> GetReceiverIds()
        {
            return receivers.Keys.ToList();
        }

        private static readonly ConcurrentDictionary<string, Receiver> receivers = new();
        private static readonly Receiver dummyReceiver = new();

        public class Receiver
        {
            public void OnAck(Action action)
            {
                this.action = action;
            }

            public void Ack()
            {
                action?.Invoke();
            }

            private Action action;
        }
    }

    public class Carrier<TMessageType> : Carrier, ICarrier<TMessageType>
    {
        public Carrier(ICarrierTransport<TMessageType> transport)
        {
            this.transport = transport;
        }

        public Task SendTo<T>(string connectionId, TMessageType messageType, T data)
        {
            return transport.SendTo(connectionId, messageType, data); ;
        }

        public Task SendToAll<T>(TMessageType messageType, T data)
        {
            return transport.SendToAll(messageType, data); ;
        }

        public async Task SendToAll<T>(TMessageType messageType, Func<string, T> getData)
        {
            foreach (var connectionId in GetReceiverIds())
            {
                await transport.SendTo(connectionId, messageType, getData(connectionId));
            }
        }

        public Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, T data)
        {
            return transport.SendToAllExcept(exceptConnectionId, messageType, data); ;
        }

        public async Task SendToAllExcept<T>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData)
        {
            foreach (var connectionId in GetReceiverIds().Where(id => id != exceptConnectionId))
            {
                await transport.SendTo(connectionId, messageType, getData(connectionId));
            }
        }

        public async Task<bool> SendToAndAwaitAck<T>(string connectionId, TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendTo(connectionId, messageType, data);
            return await ReceiveAck(connectionId, maxMs);
        }

        public void Ack(string connectionId)
        {
            GetReceiver(connectionId).Ack();
        }

        private async Task<bool> ReceiveAck(string connectionId, int maxMs)
        {
            var task = new Task(() => { });
            GetReceiver(connectionId).OnAck(() => task.Start());
            return await Task.WhenAny(task, Task.Delay(maxMs)) == task;
        }

        private readonly ICarrierTransport<TMessageType> transport;
    }
}
