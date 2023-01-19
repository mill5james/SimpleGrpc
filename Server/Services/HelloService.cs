using Grpc.Core;
using HelloWorld;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace GrpcServer;

public class HelloService : Service.ServiceBase
{
    private static Dictionary<int, CancellationTokenSource> clientMap = new Dictionary<int, CancellationTokenSource>();
    private readonly IEventGeneratorService generatorService;
    private readonly ILogger<HelloService> logger;
    public HelloService(ILogger<HelloService> logger, IEventGeneratorService generatorService)
    {
        this.generatorService = generatorService;
        this.logger = logger;
    }

    public override Task<Response> Hello(Request request, ServerCallContext context)
    {
        return Task.FromResult(new Response
        {
            Message = "Hello " + request.Name
        });
    }

    public override Task<Response> Disconnect(DisconnectRequest request, ServerCallContext context)
    {
        string msg = $"Client {request.Id} not found";
        if (clientMap.Remove(request.Id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            msg = $"Disconnected {request.Id}";
        }
        logger.LogInformation($"========= {msg}");
        return Task.FromResult(new Response{ Message = msg});
    }

    public override async Task ServerEvents(ClientIdentifer request, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
    {
        if (!clientMap.TryGetValue(request.Id, out var cts))
        {
            cts = new CancellationTokenSource();
            clientMap.Add(request.Id, cts);
        }

        using var lts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, context.CancellationToken);
        try
        {
            var eo = Observable.FromEventPattern<GeneratedEventArgs>(generatorService, nameof(generatorService.MessageEvent)) //Create Observable from event
                            .Where(evt => request.Subscriptions.Contains(evt.EventArgs.Id)); //Filter Observable to only the request subscriptions
            await eo.ForEachAsync(async evt =>  await responseStream.WriteAsync(new ServerMessage { Id = evt.EventArgs.Id, Message = evt.EventArgs.TimeStamp.ToString("o") }), lts.Token);
        }
        catch (TaskCanceledException) { /* This is expected */ }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected");
        }

        logger.LogInformation($"========= Stream Ended for {request.Id}");
    }
}
