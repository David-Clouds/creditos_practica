using CreditosPlataforma.Web.Data;
using CreditosPlataforma.Web.Models;
using CreditosPlataforma.Web.Models.ViewModels;
using CreditosPlataforma.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditosPlataforma.Web.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly UserManager<IdentityUser> _userManager;

        public AnalistaController(
            ApplicationDbContext context,
            ICacheService cacheService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _cacheService = cacheService;
            _userManager = userManager;
        }

        // GET: Analista/Index — solicitudes pendientes
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
                if (s.Cliente != null && !emails.ContainsKey(s.Cliente.UsuarioId))
                {
                    var usuario = await _userManager.FindByIdAsync(s.Cliente.UsuarioId);

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
                TempData["Error"] = "Esta solicitud ya fue evaluada anteriormente.";
                return RedirectToAction(nameof(Index));
            }

            // Regla de negocio: no aprobar si el monto supera 5x los ingresos del cliente
            if (solicitud.Cliente != null &&
                solicitud.MontoSolicitado > solicitud.Cliente.IngresosMensuales * 5)
            {
                TempData["Error"] =
                    $"No se puede aprobar: el monto (S/ {solicitud.MontoSolicitado:N2}) " +
                    $"supera 5 veces los ingresos del cliente " +
                    $"(máximo permitido: S/ {solicitud.Cliente.IngresosMensuales * 5:N2}).";

                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Aprobado;
            await _context.SaveChangesAsync();

            // Invalidar la caché del cliente
            await _cacheService.RemoveAsync(
                $"solicitudes:cliente:{solicitud.ClienteId}");

            TempData["Exito"] =
                $"Solicitud #{solicitud.Id} aprobada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Analista/Rechazar/5 (formulario para pedir el motivo)
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _context.SolicitudesCredito.FindAsync(id);

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

            var solicitud = await _context.SolicitudesCredito
                .FindAsync(modelo.SolicitudId);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] =
                    "Esta solicitud ya fue evaluada anteriormente.";

                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = modelo.MotivoRechazo;

            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync(
                $"solicitudes:cliente:{solicitud.ClienteId}");

            TempData["Exito"] =
                $"Solicitud #{solicitud.Id} rechazada.";

            return RedirectToAction(nameof(Index));
        }
    }
}
