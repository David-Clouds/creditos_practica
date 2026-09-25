using System.ComponentModel.DataAnnotations;

namespace CreditosPlataforma.Web.Models.ViewModels
{
    public class RegistroSolicitudViewModel
    {
        [Required(ErrorMessage = "Ingresa el monto a solicitar")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto solicitado")]
        public decimal MontoSolicitado { get; set; }

        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }
    }
}