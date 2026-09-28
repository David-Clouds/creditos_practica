using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CreditosPlataforma.Web.Services
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly IConfiguration _config;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> PublicarSolicitudRegistradaAsync(int solicitudId, string usuarioId, Guid? messageId = null)
        {
            var connectionString = _config["RabbitMq:ConnectionString"];
            var queueName = _config["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";
            var id = messageId ?? Guid.NewGuid();

            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(connectionString!) };
                await using var connection = await factory.CreateConnectionAsync();

                // Confirmaciones del publicador: BasicPublishAsync espera el ACK del broker
                // y lanza excepción si el broker rechaza (nack) o devuelve el mensaje.
                var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);
                await using var channel = await connection.CreateChannelAsync(options);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var mensaje = new
                {
                    Tipo = "SolicitudRegistrada",
                    MessageId = id,
                    SolicitudId = solicitudId,
                    UsuarioId = usuarioId,
                    FechaEventoUtc = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensaje));

                var props = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = id.ToString(),
                    Type = "SolicitudRegistrada"
                };

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: true,
                    basicProperties: props,
                    body: body);

                _logger.LogInformation(
                    "RABBITMQ PUBLICADO Y CONFIRMADO - SolicitudRegistrada MessageId={MessageId} SolicitudId={SolicitudId}",
                    id, solicitudId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RABBITMQ ERROR AL PUBLICAR - SolicitudId={SolicitudId} MessageId={MessageId}",
                    solicitudId, id);
                return false;
            }
        }
    }
}