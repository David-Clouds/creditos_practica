using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditosPlataforma.Web.Models
{
    public enum EstadoSolicitud
    {
        Pendiente,
        Aprobado,
        Rechazado
    }

    public class SolicitudCredito
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public Cliente? Cliente { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0")]
        public decimal MontoSolicitado { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        [MaxLength(300)]
        public string? MotivoRechazo { get; set; }
    }
}