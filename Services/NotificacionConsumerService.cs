using System.Text;
using System.Text.Json;
using CreditosPlataforma.Web.Data;
using CreditosPlataforma.Web.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CreditosPlataforma.Web.Services
{
    public class MensajeSolicitudRegistrada
    {
        public string Tipo { get; set; } = string.Empty;
        public Guid MessageId { get; set; }
        public int SolicitudId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public DateTime FechaEventoUtc { get; set; }
    }

    public class NotificacionConsumerService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacionConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public NotificacionConsumerService(
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacionConsumerService> logger)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var habilitado = _config.GetValue<bool>("RabbitMq:ConsumerEnabled");
            if (!habilitado)
            {
                _logger.LogInformation("Consumidor de RabbitMQ deshabilitado por configuración.");
                return;
            }

            var connectionString = _config["RabbitMq:ConnectionString"];
            var queueName = _config["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";

            var factory = new ConnectionFactory { Uri = new Uri(connectionString!) };
            _connection = await factory.CreateConnectionAsync(stoppingToken);

            // Consumidor manual: sin confirmaciones de publisher aquí, solo lectura con ACK explícito
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // Máximo 1 mensaje sin confirmar por vez: procesa uno, lo confirma, recién pide el siguiente
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                await ProcesarMensajeAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Consumidor de RabbitMQ escuchando en la cola '{Queue}'.", queueName);

            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }

        private async Task ProcesarMensajeAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var mensaje = JsonSerializer.Deserialize<MensajeSolicitudRegistrada>(json);

                if (mensaje == null)
                {
                    _logger.LogWarning("RABBITMQ mensaje no deserializable, se descarta (ack) para no bloquear la cola.");
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Idempotencia: si el MessageId ya fue procesado, confirma sin duplicar
                var yaExiste = await context.Notificaciones.AnyAsync(n => n.MessageId == mensaje.MessageId, stoppingToken);
                if (yaExiste)
                {
                    _logger.LogInformation("RABBITMQ mensaje duplicado MessageId={MessageId}, se descarta.", mensaje.MessageId);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                context.Notificaciones.Add(new Notificacion
                {
                    MessageId = mensaje.MessageId,
                    SolicitudId = mensaje.SolicitudId,
                    UsuarioId = mensaje.UsuarioId,
                    Texto = $"Tu solicitud #{mensaje.SolicitudId} fue registrada y está en revisión.",
                    FechaProcesamientoUtc = DateTime.UtcNow
                });

                await context.SaveChangesAsync(stoppingToken);

                // ACK manual: solo después de guardar con éxito
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

                _logger.LogInformation(
                    "RABBITMQ PROCESADO Y CONFIRMADO (ACK) - MessageId={MessageId} SolicitudId={SolicitudId}",
                    mensaje.MessageId, mensaje.SolicitudId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RABBITMQ ERROR AL PROCESAR - se reencola (nack, requeue=true)");
                // requeue=true: el broker lo vuelve a entregar (a este consumidor o a otro)
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}