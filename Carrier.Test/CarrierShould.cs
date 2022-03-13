using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Carrier.SignalR;
using Carrier.Core;
using System.Linq;
using System.Collections.Generic;

namespace CarrierTest
{
    public class CarrierShould
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var server = GetServer();

            if (SignalRCarrierFactory.TryGetCarrier<MessageType>(server.Services, out carrier))
            {
                client1 = await GetClient(server);
                client2 = await GetClient(server);
                client3 = await GetClient(server);
            }
        }

        [Test]
        public async Task SendToAll()
        {
            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendToAll(MessageType.MyMethod1, 15);

            Assert.IsTrue(await assertion1);
            Assert.IsTrue(await assertion2);
        }

        [Test]
        public async Task SendToAllGetData()
        {
            var assertion1 = client1.Expect("MyMethod1", "1");
            var assertion2 = client1.Expect("MyMethod1", "2");
            var assertion3 = client2.Expect("MyMethod1", "1");
            var assertion4 = client2.Expect("MyMethod1", "2");

            await carrier.SendToAll(MessageType.MyMethod1, id => id == client1.ConnectionId ? 1 : 2);

            Assert.IsTrue(await assertion1);
            Assert.IsFalse(await assertion2);
            Assert.IsFalse(await assertion3);
            Assert.IsTrue(await assertion4);
        }

        [Test]
        public async Task SendToOne()
        {
            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendTo(client1.ConnectionId, MessageType.MyMethod1, 15);

            Assert.IsTrue(await assertion1);
            Assert.IsFalse(await assertion2);
        }

        [Test]
        public async Task SendToAllExcept()
        {
            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendToAllExcept(client1.ConnectionId, MessageType.MyMethod1, 15);

            Assert.IsFalse(await assertion1);
            Assert.IsTrue(await assertion2);
        }

        [Test]
        public async Task SendToAllExceptGetData()
        {
            var assertion1 = client1.Expect("MyMethod1", "1");
            var assertion2 = client1.Expect("MyMethod1", "2");
            var assertion3 = client2.Expect("MyMethod1", "1");
            var assertion4 = client2.Expect("MyMethod1", "2");
            var assertion5 = client3.Expect("MyMethod1", "1");
            var assertion6 = client3.Expect("MyMethod1", "2");

            await carrier.SendToAllExcept(client3.ConnectionId, MessageType.MyMethod1, id => id == client1.ConnectionId ? 1 : 2);

            Assert.IsTrue(await assertion1);
            Assert.IsFalse(await assertion2);
            Assert.IsFalse(await assertion3);
            Assert.IsTrue(await assertion4);
            Assert.IsFalse(await assertion5);
            Assert.IsFalse(await assertion6);
        }

        [Test]
        public async Task SendToOneAndAwaitAck()
        {
            var assertion = carrier.SendToAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client1.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToTwoAwaitingAckFromOne()
        {
            var assertion1 = carrier.SendToAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            var assertion2 = carrier.SendToAndAwaitAck(client2.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client1.ConnectionId);
            Assert.True(await assertion1);
            Assert.False(await assertion2);
        }

        [Test]
        public async Task SendToOneAndAwaitAckInVain()
        {
            var assertion = carrier.SendToAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            Assert.False(await assertion);
        }

        [Test]
        public async Task SendToAllAndAwaitAck()
        {
            var assertion = carrier.SendToAllAndAwaitAck(MessageType.MyMethod1, 15);
            carrier.Ack(client1.ConnectionId);
            carrier.Ack(client2.ConnectionId);
            carrier.Ack(client3.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToAllAndAwaitAckInVainForOne()
        {
            var assertion = carrier.SendToAllAndAwaitAck(MessageType.MyMethod1, 15);
            carrier.Ack(client1.ConnectionId);
            carrier.Ack(client2.ConnectionId);
            Assert.False(await assertion);
        }

        [Test]
        public async Task SendToAllAndAwaitAckGetData()
        {
            var assertion = carrier.SendToAllAndAwaitAck(MessageType.MyMethod1, id => id);
            carrier.Ack(client1.ConnectionId);
            carrier.Ack(client2.ConnectionId);
            carrier.Ack(client3.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToAllAndAwaitAckInVainForOneGetData()
        {
            var assertion = carrier.SendToAllAndAwaitAck(MessageType.MyMethod1, id => id);
            carrier.Ack(client1.ConnectionId);
            carrier.Ack(client2.ConnectionId);
            Assert.False(await assertion);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAck()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client2.ConnectionId);
            carrier.Ack(client3.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAckInVainForOne()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client2.ConnectionId);
            Assert.False(await assertion);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAckGetData()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, id => id);
            carrier.Ack(client2.ConnectionId);
            carrier.Ack(client3.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAckInVainForOneGetData()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, id => id);
            carrier.Ack(client2.ConnectionId);
            Assert.False(await assertion);
        }

        [Test]
        public async Task SendToOneAndAwaitAnswer()
        {
            var assertion = carrier.SendToAndAwaitAnswer<int, int>(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "15");
            (bool ok, int? value) = await assertion;
            Assert.True(ok);
            Assert.AreEqual(15, value);
        }

        [Test]
        public async Task SendToTwoAwaitingAnswerFromOne()
        {
            var assertion1 = carrier.SendToAndAwaitAnswer<int,int>(client1.ConnectionId, MessageType.MyMethod1, 15);
            var assertion2 = carrier.SendToAndAwaitAnswer<int, int>(client2.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "15");
            (bool ok1, int? _) = await assertion1;
            (bool ok2, int? _) = await assertion2;
            Assert.True(ok1);
            Assert.False(ok2);
        }

        [Test]
        public async Task SendToOneAndAwaitAnswerInVain()
        {
            var assertion = carrier.SendToAndAwaitAnswer<int, int>(client1.ConnectionId, MessageType.MyMethod1, 15);
            (bool ok, int? _) = await assertion;
            Assert.False(ok);
        }

        [Test]
        public async Task SendToAllAndAwaitAnswer()
        {
            var assertion = carrier.SendToAllAndAwaitAnswer<int, int>(MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client2.ConnectionId, "102");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(3, answers.Count);
            Assert.AreEqual(101, answers[client1.ConnectionId]);
            Assert.AreEqual(102, answers[client2.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllAndAwaitAnswerInVainForOne()
        {
            var assertion = carrier.SendToAllAndAwaitAnswer<int, int>(MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(2, answers.Count);
            Assert.AreEqual(101, answers[client1.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllAndAwaitAnswerGetData()
        {
            var assertion = carrier.SendToAllAndAwaitAnswer<string, int>(MessageType.MyMethod1, id => id);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client2.ConnectionId, "102");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(3, answers.Count);
            Assert.AreEqual(101, answers[client1.ConnectionId]);
            Assert.AreEqual(102, answers[client2.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllAndAwaitAnswerInVainForOneGetData()
        {
            var assertion = carrier.SendToAllAndAwaitAnswer<string, int>(MessageType.MyMethod1, id => id);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(2, answers.Count);
            Assert.AreEqual(101, answers[client1.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAnswer()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAnswer<int, int>(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client2.ConnectionId, "102");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(2, answers.Count);
            Assert.AreEqual(102, answers[client2.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAnswerInVainForOne()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAnswer<int, int>(client1.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(1, answers.Count);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAnswerGetData()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAnswer<string, int>(client1.ConnectionId, MessageType.MyMethod1, id => id);
            carrier.Answer(client1.ConnectionId, "101");
            carrier.Answer(client2.ConnectionId, "102");
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(2, answers.Count);
            Assert.AreEqual(102, answers[client2.ConnectionId]);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }

        [Test]
        public async Task SendToAllExceptAndAwaitAnswerInVainForOneGetData()
        {
            var assertion = carrier.SendToAllExceptAndAwaitAnswer<string, int>(client1.ConnectionId, MessageType.MyMethod1, id => id);
            carrier.Answer(client3.ConnectionId, "103");
            var answers = await assertion;
            Assert.AreEqual(1, answers.Count);
            Assert.AreEqual(103, answers[client3.ConnectionId]);
        }


        private static TestServer GetServer()
        {
            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSignalRCarrier();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapCarrierHub();
                    });
                });
            return new TestServer(webHostBuilder);
        }

        private static async Task<HubConnection> GetClient(TestServer server)
        {
            var client = new HubConnectionBuilder()
                .WithUrl("http://localhost/carrierhub", o => o.HttpMessageHandlerFactory = _ => server.CreateHandler())
                .Build();
            await client.StartAsync();
            return client;
        }

        private HubConnection client1, client2, client3;
        private ICarrier<MessageType> carrier;
    }

    public enum MessageType { MyMethod1, MyMethod2 };
}