using CreditosPlataforma.Web.Models;

namespace CreditosPlataforma.Web.Models.ViewModels
{
    public class SolicitudFiltroViewModel
    {
        public EstadoSolicitud? Estado { get; set; }
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public List<SolicitudCredito> Resultados { get; set; } = new();
        public string? MensajeError { get; set; }
    }
}