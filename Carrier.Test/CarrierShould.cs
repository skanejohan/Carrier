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
        [SetUp]
        public void Setup()
        {
            server = GetServer();
           
            carrier = SignalRCarrierFactory.GetCarrier<MessageType>(server.Services);
        }

        [Test]
        public async Task SendToAll()
        {
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);

            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendToAll(MessageType.MyMethod1, 15);

            Assert.IsTrue(await assertion1);
            Assert.IsTrue(await assertion2);
        }

        [Test]
        public async Task SendToAllGetData()
        {
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);

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
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);

            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendTo(client1.ConnectionId, MessageType.MyMethod1, 15);

            Assert.IsTrue(await assertion1);
            Assert.IsFalse(await assertion2);
        }

        [Test]
        public async Task SendToAllExcept()
        {
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);

            var assertion1 = client1.Expect("MyMethod1", "15");
            var assertion2 = client2.Expect("MyMethod1", "15");

            await carrier.SendToAllExcept(client1.ConnectionId, MessageType.MyMethod1, 15);

            Assert.IsFalse(await assertion1);
            Assert.IsTrue(await assertion2);
        }

        [Test]
        public async Task SendToAllExceptGetData()
        {
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);
            var client3 = await GetClient(server);

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
            var client = await GetClient(server);
            var assertion = carrier.SendToAndAwaitAck(client.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client.ConnectionId);
            Assert.True(await assertion);
        }

        [Test]
        public async Task SendToTwoAwaitingAckFromOne()
        {
            var client1 = await GetClient(server);
            var client2 = await GetClient(server);
            var assertion1 = carrier.SendToAndAwaitAck(client1.ConnectionId, MessageType.MyMethod1, 15);
            var assertion2 = carrier.SendToAndAwaitAck(client2.ConnectionId, MessageType.MyMethod1, 15);
            carrier.Ack(client1.ConnectionId);
            Assert.True(await assertion1);
            Assert.False(await assertion2);
        }

        [Test]
        public async Task SendToOneAndAwaitAckInVain()
        {
            var client = await GetClient(server);
            var assertion = carrier.SendToAndAwaitAck(client.ConnectionId, MessageType.MyMethod1, 15);
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

        private ICarrier<MessageType> carrier;
        private TestServer server;
    }

    public enum MessageType { MyMethod1, MyMethod2 };
}