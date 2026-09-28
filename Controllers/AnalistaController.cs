using CreditosPlataforma.Web.Data;
using CreditosPlataforma.Web.Models;
using CreditosPlataforma.Web.Models.ViewModels;
using CreditosPlataforma.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CreditosPlataforma.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CreditosPlataforma.Web.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<SolicitudesHub> _hubContext;

        public AnalistaController(
            ApplicationDbContext context,
            ICacheService cacheService,
            UserManager<IdentityUser> userManager,
            IHubContext<SolicitudesHub> hubContext)
        {
            _context = context;
            _cacheService = cacheService;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // GET: Analista/Index
        // Solicitudes pendientes
        public async Task<IActionResult> Index()
        {
            var pendientes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            var emails = new Dictionary<string, string>();

            foreach (var s in pendientes)
            {
                if (s.Cliente != null &&
                    !emails.ContainsKey(s.Cliente.UsuarioId))
                {
                    var usuario = await _userManager
                        .FindByIdAsync(s.Cliente.UsuarioId);

                    emails[s.Cliente.UsuarioId] =
                        usuario?.Email ?? s.Cliente.UsuarioId;
                }
            }

            ViewBag.Emails = emails;

            return View(pendientes);
        }

        // POST: Analista/Aprobar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] =
                    "Esta solicitud ya fue evaluada anteriormente.";

                return RedirectToAction(nameof(Index));
            }

            // Regla de negocio:
            // No aprobar si el monto supera 5 veces
            // los ingresos mensuales del cliente.
            if (solicitud.Cliente != null &&
                solicitud.MontoSolicitado >
                solicitud.Cliente.IngresosMensuales * 5)
            {
                TempData["Error"] =
                    $"No se puede aprobar: el monto " +
                    $"(S/ {solicitud.MontoSolicitado:N2}) " +
                    $"supera 5 veces los ingresos del cliente " +
                    $"(máximo permitido: S/ " +
                    $"{solicitud.Cliente.IngresosMensuales * 5:N2}).";

                return RedirectToAction(nameof(Index));
            }

            // Aprobar solicitud
            solicitud.Estado = EstadoSolicitud.Aprobado;

            await _context.SaveChangesAsync();

            // Invalidar la caché del cliente
            await _cacheService.RemoveAsync(
                $"solicitudes:cliente:{solicitud.ClienteId}");

            // Notificar al cliente mediante SignalR
            if (solicitud.Cliente != null)
            {
                await _hubContext.Clients
                    .Group(solicitud.Cliente.UsuarioId)
                    .SendAsync(
                        "NotificacionSolicitud",
                        new
                        {
                            solicitudId = solicitud.Id,
                            estado = "Aprobado",
                            mensaje =
                                $"Tu solicitud #{solicitud.Id} fue aprobada."
                        });
            }

            TempData["Exito"] =
                $"Solicitud #{solicitud.Id} aprobada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Analista/Rechazar/5
        // Formulario para solicitar el motivo del rechazo
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .FindAsync(id);

            if (solicitud == null ||
                solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                return NotFound();
            }

            return View(
                new DecisionSolicitudViewModel
                {
                    SolicitudId = id
                });
        }

        // POST: Analista/Rechazar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(
            DecisionSolicitudViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            // Obtener solicitud junto con el cliente
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(
                    s => s.Id == modelo.SolicitudId);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] =
                    "Esta solicitud ya fue evaluada anteriormente.";

                return RedirectToAction(nameof(Index));
            }

            // Rechazar solicitud y guardar motivo
            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = modelo.MotivoRechazo;

            await _context.SaveChangesAsync();

            // Invalidar la caché del cliente
            await _cacheService.RemoveAsync(
                $"solicitudes:cliente:{solicitud.ClienteId}");

            // Notificar al cliente mediante SignalR
            if (solicitud.Cliente != null)
            {
                await _hubContext.Clients
                    .Group(solicitud.Cliente.UsuarioId)
                    .SendAsync(
                        "NotificacionSolicitud",
                        new
                        {
                            solicitudId = solicitud.Id,
                            estado = "Rechazado",
                            mensaje =
                                $"Tu solicitud #{solicitud.Id} fue rechazada: " +
                                $"{modelo.MotivoRechazo}"
                        });
            }

            TempData["Exito"] =
                $"Solicitud #{solicitud.Id} rechazada.";

            return RedirectToAction(nameof(Index));
        }
    }
}
