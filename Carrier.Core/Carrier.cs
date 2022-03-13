﻿using System.Collections.Concurrent;
using System.Text.Json;

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
            var (ok, result) = await ReceiveAnswer(connectionId, maxMs);
            return ok ? (ok, JsonSerializer.Deserialize<T2>(result)) : (ok, default(T2));
        }

        public Task<IEnumerable<T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, T data, int maxMs = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T2>> SendToAllAndAwaitAnswer<T, T2>(TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, T data, int maxMs = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T2>> SendToAllExceptAndAwaitAnswer<T, T2>(string exceptConnectionId, TMessageType messageType, Func<string, T> getData, int maxMs = 5000)
        {
            throw new NotImplementedException();
        }

        public void Ack(string connectionId)
        {
            GetReceiver(connectionId).Ack();
        }

        public void Answer(string connectionId, string json)
        {
            GetReceiver(connectionId).Answer(json);
        }

        private static async Task<bool> ReceiveAck(string connectionId, int maxMs)
        {
            var task = new Task(() => { });
            GetReceiver(connectionId).OnAck(() => task.StartSafe());
            return await Task.WhenAny(task, Task.Delay(maxMs)) == task;
        }

        private static async Task<bool> ReceiveAcks(IEnumerable<string> connectionIds, int maxMs)
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

        private static async Task<(bool,string)> ReceiveAnswer(string connectionId, int maxMs)
        {
            var answer = "";
            var task = new Task(() => { });
            GetReceiver(connectionId).OnAnswer(
                a =>
                {
                    answer = a;
                    task.StartSafe();
                });
            return (await Task.WhenAny(task, Task.Delay(maxMs)) == task, answer);
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
