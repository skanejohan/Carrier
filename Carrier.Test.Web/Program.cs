using Carrier.SignalR;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

List<string> clients = new();
string response = "";

void UpdateUI()
{
    Console.Clear();
    Console.WriteLine($"No of clients: {clients.Count}");
    if (clients.Count > 0)
    {
        Console.WriteLine($"1: request ack from {clients[0]}");
        Console.WriteLine($"2: request answer from {clients[0]}");
    }
    if (clients.Count > 1)
    {
        Console.WriteLine($"3: request ack from {clients[1]}");
        Console.WriteLine($"4: request answer from {clients[1]}");
    }
    if (clients.Count > 0)
    {
        Console.WriteLine($"A: request ack from all");
        Console.WriteLine($"B: request answer from all");
    }
    Console.WriteLine("Q: Quit");
    Console.WriteLine();
    Console.WriteLine(response);
}

void ClientsUpdated(IEnumerable<string> clientIds)
{
    clients = clientIds.ToList();
    UpdateUI();
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalRCarrier();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapCarrierHub();

SignalRCarrierFactory.TryGetCarrier<string>(app.Services, out var carrier, out var carrierMonitor);
if (carrierMonitor is not null)
{
    carrierMonitor.OnClientsUpdated = ClientsUpdated;
}

app.MapPost("/ack/{id}", (string id) => carrier?.Ack(id));
app.MapPost("/answer/{id}", (string id, [FromBody] Answer json) =>
{
    carrier?.Answer(id, JsonSerializer.Serialize<Answer>(json));
});

app.RunAsync();

Console.Clear();

while(true)
{
    try
    {
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.D1:
                response = await carrier?.SendToAndAwaitAck(clients[0], "ack", "Please acknowledge") 
                    ? $"SendToAndAwaitAck({clients[0]}) succeeded"
                    : $"SendToAndAwaitAck({clients[0]}) failed";
                break;
            case ConsoleKey.D2:
                var (ok, answer) = await carrier?.SendToAndAwaitAnswer<string, Answer>(clients[0], "answer", "Please answer");
                response = ok
                    ? $"SendToAndAwaitAnswer({clients[0]}) succeeded: {answer}"
                    : $"SendToAndAwaitAnswer({clients[0]}) failed";
                break;
            case ConsoleKey.D3:
                response = await carrier?.SendToAndAwaitAck(clients[1], "ack", "Please acknowledge")
                    ? $"SendToAndAwaitAck({clients[1]}) succeeded"
                    : $"SendToAndAwaitAck({clients[1]}) failed";
                break;
            case ConsoleKey.D4:
                (ok, answer) = await carrier?.SendToAndAwaitAnswer<string, Answer>(clients[1], "answer", "Please answer");
                response = ok
                    ? $"SendToAndAwaitAnswer({clients[1]}) succeeded: {answer}"
                    : $"SendToAndAwaitAnswer({clients[1]}) failed";
                break;
            case ConsoleKey.A:
                ok = await carrier?.SendToAllAndAwaitAck<string>("ack", "Please acknowledge");
                response = ok
                    ? "SendToAllAndAwaitAck succeeded"
                    : "SendToAllAndAwaitAck failed";
                break;
            case ConsoleKey.B:
                var dict = await carrier?.SendToAllAndAwaitAnswer<string, Answer>("answer", "Please answer");
                response = string.Join(";", dict.Select(x => x.Key + "=" + x.Value).ToArray());
                break;

            case ConsoleKey.Q:
                Environment.Exit(0);
                break;
            default:
                break;
        }
    }
    catch (Exception e)
    {
        response = $"Exception {e.GetType()} with message {e.Message}";
    }
    UpdateUI();
}

public class Answer
{
    public string Id { get; set; }
    public int TrueAnswer { get; set; }
    public override string ToString()
    {
        return JsonSerializer.Serialize<Answer>(this);
    }
}
