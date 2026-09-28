using System.ComponentModel.DataAnnotations;

namespace CreditosPlataforma.Web.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        public Guid MessageId { get; set; }

        public int SolicitudId { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaProcesamientoUtc { get; set; }
    }
}