using System.ComponentModel.DataAnnotations;

namespace CreditosPlataforma.Web.Models.ViewModels
{
    public class DecisionSolicitudViewModel
    {
        public int SolicitudId { get; set; }

        [Required(ErrorMessage = "Debes indicar un motivo de rechazo")]
        [MaxLength(300)]
        [Display(Name = "Motivo de rechazo")]
        public string MotivoRechazo { get; set; } = string.Empty;
    }
}