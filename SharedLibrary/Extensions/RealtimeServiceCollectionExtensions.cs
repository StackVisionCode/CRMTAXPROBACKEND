using System.Text.Json;
using MassTransit;
using MassTransit.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Services.RabbitMQ;

namespace SharedLibrary.Extensions;

public static class RealtimeServiceCollectionExtensions
{
    /// <summary>
    /// Registra SignalR + MassTransit 7.3 como back-plane RabbitMQ.
    /// </summary>
    public static IServiceCollection AddRealtimeComm<THub>(
        this IServiceCollection services,
        IConfiguration cfg
    )
        where THub : Hub
    {
        /*─────────────────────────────
         * 1. SignalR (solo protocolos)
         *────────────────────────────*/
        services
            .AddSignalR()
            .AddJsonProtocol(p =>
            {
                var opts = p.PayloadSerializerOptions;
                opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                opts.PropertyNameCaseInsensitive = true;
            })
            .AddMessagePackProtocol(); // opcional

        /*─────────────────────────────
         * 2. MassTransit 7.3
         *────────────────────────────*/
        services.AddMassTransit(x =>
        {
            /* 2-a) publica/consume THub en el bus */
            x.AddSignalRHub<THub>();

            /* 2-b) transporte RabbitMQ */
            x.UsingRabbitMq(
                (context, cfgBus) =>
                {
                    var opts =
                        cfg.GetSection("RabbitMQ").Get<RabbitMQOptions>()
                        ?? throw new InvalidOperationException("RabbitMQ config missing");

                    var uri = new Uri($"rabbitmq://{opts.HostName}:{opts.Port}{opts.VirtualHost}");
                    cfgBus.Host(
                        uri,
                        h =>
                        {
                            h.Username(opts.UserName);
                            h.Password(opts.Password);
                        }
                    );

                    /* 2-c) 👉 EN 7.x SIEMPRE  ConfigureEndpoints  */
                    cfgBus.ConfigureEndpoints(context); // crea cola signalr.hub-…
                }
            );
        });

        return services;
    }
}
