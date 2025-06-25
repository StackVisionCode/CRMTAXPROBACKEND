using MassTransit;
using MassTransit.SignalR;
using Microsoft.AspNetCore.SignalR; // Para 'Hub'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Services.RabbitMQ;

namespace SharedLibrary.Extensions;

public static class RealtimeServiceCollectionExtensions
{
    // THub debe heredar de Hub
    public static IServiceCollection AddRealtimeComm<THub>(
        this IServiceCollection services,
        IConfiguration cfg
    )
        where THub : Hub
    {
        // SignalR server + MessagePack
        services.AddSignalR().AddMessagePackProtocol();

        services.AddMassTransit(x =>
        {
            // back-plane: MassTransit publica/consume los mensajes del hub
            x.AddSignalRHub<THub>();

            x.UsingRabbitMq(
                (context, busCfg) =>
                {
                    var opts = cfg.GetSection("RabbitMQ").Get<RabbitMQOptions>();
                    if (opts == null)
                    {
                        throw new InvalidOperationException(
                            "RabbitMQ configuration section is missing or invalid."
                        );
                    }

                    // Formato URI incluye puerto y vhost
                    var uri = new Uri($"rabbitmq://{opts.HostName}:{opts.Port}{opts.VirtualHost}");
                    busCfg.Host(
                        uri,
                        h =>
                        {
                            h.Username(opts.UserName);
                            h.Password(opts.Password);
                        }
                    );
                }
            );
        });

        return services;
    }
}
