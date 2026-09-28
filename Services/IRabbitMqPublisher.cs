namespace CreditosPlataforma.Web.Services
{
    public interface IRabbitMqPublisher
    {
        // Devuelve true si el broker confirmó la recepción; false si falló.
        // messageId opcional: permite reenviar un mensaje con el mismo MessageId.
        Task<bool> PublicarSolicitudRegistradaAsync(int solicitudId, string usuarioId, Guid? messageId = null);
    }
}