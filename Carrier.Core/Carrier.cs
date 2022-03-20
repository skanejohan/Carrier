using System.Collections.Concurrent;
using System.Text.Json;

namespace Carrier.Core
{
    public class Carrier : ICarrierMonitor
    {
        public static Carrier? Instance => instance;

        public Action<IEnumerable<string>>? OnClientsUpdated { get; set; }

        public void AddReceiver(string id)
        {
            lock(receivers)
            {
                receivers[id] = new Receiver();
                OnClientsUpdated?.Invoke(GetReceiverIds());
            }
        }

        public void RemoveReceiver(string id)
        {
            lock (receivers)
            {
                if (receivers.TryRemove(id, out var _))
                {
                    OnClientsUpdated?.Invoke(GetReceiverIds());
                }
            }
        }

        protected Receiver GetReceiver(string id)
        {
            return receivers[id] ?? dummyReceiver;
        }

        protected IEnumerable<string> GetReceiverIds()
        {
            return receivers.Keys.ToList();
        }

        private readonly ConcurrentDictionary<string, Receiver> receivers = new();
        private readonly Receiver dummyReceiver = new();
        protected static Carrier? instance;

        public class Receiver
        {
            public void OnAck(Action action)
            {
                this.ackAction = action;
            }

            public void OnAnswer(Action<string> action)
            {
                this.answerAction = action;
            }

            public void Ack()
            {
                ackAction.Invoke();
            }

            public void Answer(string json)
            {
                answerAction.Invoke(json);
            }

            private Action ackAction = () => { };
            private Action<string> answerAction = s => { };
        }
    }

    public class Carrier<TMessageType> : Carrier, ICarrier<TMessageType>
    {
        public static Carrier<TMessageType>? Create(ICarrierTransport<TMessageType> transport)
        {
            if (instance == null)
            {
                instance = new Carrier<TMessageType>(transport);
            }
            return instance as Carrier<TMessageType>;
        }

        private Carrier(ICarrierTransport<TMessageType> transport)
        {
            this.transport = transport;
        }

        public Task SendTo<T>(string connectionId, TMessageType messageType, T data)
        {
            return transport.SendTo(connectionId, messageType, data);
        }

        public async Task SendToAll<T>(TMessageType messageType, T data)
        {
            foreach (var connectionId in GetReceiverIds())
            {
                await transport.SendTo(connectionId, messageType, data);
            }
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

        public async Task<bool> SendToAllAndAwaitAck<T>(TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendToAll(messageType, data);
            return await ReceiveAcks(GetReceiverIds(), maxMs);
        }

        public async Task<bool> SendToAllAndAwaitAck<T>(TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            await SendToAll(messageType, getData);
            return await ReceiveAcks(GetReceiverIds(), maxMs);
        }

        public async Task<bool> SendToAllExceptAndAwaitAck<T>(string exceptConnectionId, TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendToAllExcept(exceptConnectionId, messageType, data);
            return await ReceiveAcks(GetReceiverIds().Where(id => id != exceptConnectionId), maxMs);
        }

        public async Task<bool> SendToAllExceptAndAwaitAck<T>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            await SendToAllExcept(exceptConnectionId, messageType, getData);
            return await ReceiveAcks(GetReceiverIds().Where(id => id != exceptConnectionId), maxMs);
        }

        public async Task<(bool, T2?)> SendToAndAwaitAnswer<T, T2>(string connectionId, TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendTo(connectionId, messageType, data);
            var (ok, result) = await ReceiveAnswer<T2>(connectionId, maxMs);
            return ok ? (ok, result) : (ok, default(T2));
        }

        public async Task<Dictionary<string, T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendToAll(messageType, data);
            return await ReceiveAnswers<T2>(GetReceiverIds(), maxMs);
        }

        public async Task<Dictionary<string, T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            await SendToAll(messageType, getData);
            return await ReceiveAnswers<T2>(GetReceiverIds(), maxMs);
        }

        public async Task<Dictionary<string, T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, T data, int maxMs = 5000)
        {
            await SendToAllExcept(exceptConnectionId, messageType, data);
            return await ReceiveAnswers<T2>(GetReceiverIds().Where(id => id != exceptConnectionId), maxMs);
        }

        public async Task<Dictionary<string, T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            await SendToAllExcept(exceptConnectionId, messageType, getData);
            return await ReceiveAnswers<T2>(GetReceiverIds().Where(id => id != exceptConnectionId), maxMs);
        }

        public void Ack(string connectionId)
        {
            GetReceiver(connectionId).Ack();
        }

        public void Answer(string connectionId, string json)
        {
            GetReceiver(connectionId).Answer(json);
        }

        private async Task<bool> ReceiveAck(string connectionId, int maxMs)
        {
            var task = new Task(() => { });
            GetReceiver(connectionId).OnAck(() => task.StartSafe());
            return await Task.WhenAny(task, Task.Delay(maxMs)) == task;
        }

        private async Task<bool> ReceiveAcks(IEnumerable<string> connectionIds, int maxMs)
        {
            var tasks = connectionIds.Select(connectionId =>
            {
                var task = new Task(() => { });
                GetReceiver(connectionId).OnAck(() => task.StartSafe());
                return task;
            });
            var delayTask = Task.Delay(maxMs);
            return await Task.WhenAny(Task.WhenAll(tasks), delayTask) != delayTask;
        }

        private async Task<(bool, T2)> ReceiveAnswer<T2>(string connectionId, int maxMs)
        {
            var answer = default(T2);
            var task = new Task(() => { });
            GetReceiver(connectionId).OnAnswer(
                a =>
                {
                    answer = JsonSerializer.Deserialize<T2>(a);
                    task.StartSafe();
                });
            return (await Task.WhenAny(task, Task.Delay(maxMs)) == task, answer);
        }

        private async Task<Dictionary<string, T2>> ReceiveAnswers<T2>(IEnumerable<string> connectionIds, int maxMs)
        {
            var answers = new ConcurrentDictionary<string, T2>();
            var tasks = connectionIds.Select(connectionId =>
            {
                var task = new Task(() => { });
                GetReceiver(connectionId).OnAnswer(
                    json =>
                    {
                        var answer = JsonSerializer.Deserialize<T2>(json);
                        if (answer is not null)
                        {
                            answers[connectionId] = answer;
                        }
                        task.StartSafe();
                    });
                return task;
            });
            var delayTask = Task.Delay(maxMs);
            await Task.WhenAny(Task.WhenAll(tasks), delayTask);
            return answers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, answers.Comparer);
        }

        private readonly ICarrierTransport<TMessageType> transport;
    }

    public static class TaskExtensions
    {
        public static void StartSafe(this Task task)
        {
            if (task.Status == TaskStatus.Created) 
            { 
                task.Start(); 
            }
        }
    }
}
