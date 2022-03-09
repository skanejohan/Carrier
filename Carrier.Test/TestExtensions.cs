using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace CarrierTest
{
    public static class TestExtensions
    {
        public static async Task<bool> Expect(this HubConnection client, string methodName, string data, int maxMs = 3000)
        {
            var task = new Task(() => { });
            client.On<string>(methodName, msg =>
            {
                if (msg == data)
                {
                    task.Start();
                }
            });
            return await Task.WhenAny(task, Task.Delay(maxMs)) == task;
        }
    }
}