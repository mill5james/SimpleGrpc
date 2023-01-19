//Added NuGet Package Grpc.Net.Client
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;
using HelloWorld;
using Grpc.Core;
using System.Threading;
using System.Linq;

namespace GrpcClient;

public class Program
{
    static async Task Main(string[] args)
    {
        var rng = new Random();
        var channel = GrpcChannel.ForAddress("https://localhost:5001");
        var client = new Service.ServiceClient(channel);

        var id = rng.Next();

        using (var tokenSource = new CancellationTokenSource())
        {
            //Run KeepAlive in background
            _ = Task.Run(async () => await ServerEvents(id, client, tokenSource.Token));
            //Call unary method
            var reply = await client.HelloAsync(new Request { Name = $"call" });
            Console.WriteLine("Greeting: " + reply.Message);
            Console.WriteLine("Press any key to disconnect...");
            Console.ReadLine();
            var disco = await client.DisconnectAsync(new DisconnectRequest{ Id = id });
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }

    static async Task ServerEvents(int id, Service.ServiceClient client, CancellationToken token)
    {
        ClientIdentifer clientIdentifer = new ClientIdentifer { Id = id };
        var rng = new Random();

        clientIdentifer.Subscriptions.AddRange(Enumerable.Range(0, rng.Next(2,20)).Select(_ => rng.Next(20)).Distinct());
        // clientIdentifer.Subscriptions.AddRange(Enumerable.Range(0, 20));

        Console.WriteLine($"ID:\t{clientIdentifer.Id}");
        Console.WriteLine($"Subs:\t{string.Join(",", clientIdentifer.Subscriptions.OrderBy(_ => _))}");

        using (AsyncServerStreamingCall<ServerMessage> keepAliveCall = client.ServerEvents(clientIdentifer, cancellationToken: token))
        {
            await foreach (var msg in keepAliveCall.ResponseStream.ReadAllAsync(token))
            {
                if (msg.Id == -1) break;
                Console.WriteLine($"{msg.Id} - {msg.Message}");
            }
        }
        Console.WriteLine("=== Stream Complete ===");
    }
}

