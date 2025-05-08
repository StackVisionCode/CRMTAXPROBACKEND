using System.Text;
using System.Text.Json;
using CustomerService.DTOs.AuthEvents;
using CustomerService.Infrastructure.Configuration;
using CustomerService.Infrastructure.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CustomerService.Applications.Services;

public sealed class LoginEventConsumer : BackgroundService, IMessageConsumerService
{
    private readonly IModel  _ch;
    private readonly ILogger<LoginEventConsumer> _log;
    private readonly IServiceProvider _provider;

    public LoginEventConsumer(IOptions<RabbitSettings> opt,
                              ILogger<LoginEventConsumer>  log,
                              IServiceProvider             provider)
    {
        _log      = log;
        _provider = provider;

        var f = new ConnectionFactory
        {
            HostName  = opt.Value.Host,
            UserName  = opt.Value.User,
            Password  = opt.Value.Pass,
            DispatchConsumersAsync = true
        };

        _ch = f.CreateConnection().CreateModel();

        _ch.ExchangeDeclare(RabbitSettings.AuthExchange, ExchangeType.Topic, durable:true);
        _ch.QueueDeclare (RabbitSettings.LoginEventsQueue, durable:true, exclusive:false, autoDelete:false);
        _ch.QueueBind    (RabbitSettings.LoginEventsQueue, RabbitSettings.AuthExchange, RabbitSettings.LoginEventRoutingKey);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartConsuming();
        return Task.CompletedTask;
    }

    public void StartConsuming()
    {
        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.Received += OnMessage;
        _ch.BasicConsume(RabbitSettings.LoginEventsQueue, autoAck:false, consumer);
        _log.LogInformation("⏳ Waiting for login events …");
    }

    private Task OnMessage(object? sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var ev   = JsonSerializer.Deserialize<LoginEventDTO>(json);

            _log.LogInformation("✔ Login event from {Email}", ev?.Email);

            // aquí tu lógica (*scope* requerido si inyectas servicios de EF, etc.)
            using var scope = _provider.CreateScope();
            // Ejemplo: guardar en base de datos …

            _ch.BasicAck(ea.DeliveryTag, multiple:false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,"❌ Error processing login event");
            _ch.BasicNack(ea.DeliveryTag, multiple:false, requeue:true);
        }

        return Task.CompletedTask;
    }

    public void StopConsuming()
    {
        _ch?.Close();
    }

    public override void Dispose()
    {
        _ch?.Dispose();
        base.Dispose();
    }
}