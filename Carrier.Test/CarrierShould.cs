using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Carrier.SignalR;
using Carrier.Core;

namespace CarrierTest
{
    public class CarrierShould
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var server = GetServer();

            carrier = SignalRCarrierFactory.GetCarrier<MessageType>(server.Services);
            client1 = await GetClient(server);
            client2 = await GetClient(server);
            client3 = await GetClient(server);
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