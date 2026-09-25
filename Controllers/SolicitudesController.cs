using CreditosPlataforma.Web.Data;
using CreditosPlataforma.Web.Models;
using CreditosPlataforma.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditosPlataforma.Web.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Solicitudes/Index (Mis solicitudes)
        public async Task<IActionResult> Index(SolicitudFiltroViewModel filtro)
        {
            var usuarioId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (cliente == null)
            {
                filtro.MensajeError = "No se encontró un cliente asociado a tu usuario.";
                return View(filtro);
            }

            // Validación server-side de rangos
            if (filtro.MontoMinimo.HasValue && filtro.MontoMinimo < 0)
            {
                filtro.MensajeError = "El monto mínimo no puede ser negativo.";
                return View(filtro);
            }

            if (filtro.MontoMaximo.HasValue && filtro.MontoMaximo < 0)
            {
                filtro.MensajeError = "El monto máximo no puede ser negativo.";
                return View(filtro);
            }

            if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio > filtro.FechaFin)
            {
                filtro.MensajeError = "La fecha de inicio no puede ser mayor a la fecha fin.";
                return View(filtro);
            }

            var query = _context.SolicitudesCredito
                .Where(s => s.ClienteId == cliente.Id)
                .AsQueryable();

            if (filtro.Estado.HasValue)
                query = query.Where(s => s.Estado == filtro.Estado.Value);

            if (filtro.MontoMinimo.HasValue)
                query = query.Where(s => s.MontoSolicitado >= filtro.MontoMinimo.Value);

            if (filtro.MontoMaximo.HasValue)
                query = query.Where(s => s.MontoSolicitado <= filtro.MontoMaximo.Value);

            if (filtro.FechaInicio.HasValue)
                query = query.Where(s => s.FechaSolicitud >= filtro.FechaInicio.Value);

            if (filtro.FechaFin.HasValue)
                query = query.Where(s => s.FechaSolicitud <= filtro.FechaFin.Value);

            filtro.Resultados = await query
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(filtro);
        }

        // GET: Solicitudes/Registrar
        public IActionResult Registrar()
        {
            return View(new RegistroSolicitudViewModel());
        }

        // POST: Solicitudes/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistroSolicitudViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuarioId = _userManager.GetUserId(User);

            // Usuario debe estar autenticado
            if (string.IsNullOrEmpty(usuarioId))
            {
                modelo.MensajeError = "No se pudo identificar al usuario autenticado.";
                return View(modelo);
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (cliente == null)
            {
                modelo.MensajeError = "No se encontró un cliente asociado a tu usuario.";
                return View(modelo);
            }

            // Cliente debe estar activo
            if (!cliente.Activo)
            {
                modelo.MensajeError = "Tu cuenta de cliente no está activa. Contacta con soporte.";
                return View(modelo);
            }

            // No permitir más de una solicitud Pendiente por cliente
            var tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s =>
                    s.ClienteId == cliente.Id &&
                    s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                modelo.MensajeError = "Ya tienes una solicitud pendiente. Espera a que sea evaluada antes de registrar otra.";
                return View(modelo);
            }

            // El monto no puede superar 10x los ingresos mensuales
            if (modelo.MontoSolicitado > cliente.IngresosMensuales * 10)
            {
                modelo.MensajeError =
                    $"El monto solicitado no puede superar 10 veces tus ingresos mensuales " +
                    $"(máximo: S/ {cliente.IngresosMensuales * 10:N2}).";

                return View(modelo);
            }

            var solicitud = new SolicitudCredito
            {
                ClienteId = cliente.Id,
                MontoSolicitado = modelo.MontoSolicitado,
                FechaSolicitud = DateTime.UtcNow,
                Estado = EstadoSolicitud.Pendiente
            };

            _context.SolicitudesCredito.Add(solicitud);
            await _context.SaveChangesAsync();

            // Limpiamos el modelo para la siguiente solicitud, pero mostramos éxito
            return View(new RegistroSolicitudViewModel
            {
                MensajeExito = "¡Solicitud registrada correctamente! Quedó en estado Pendiente."
            });
        }

        // GET: Solicitudes/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = _userManager.GetUserId(User);

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Cliente?.UsuarioId != usuarioId)
            {
                return NotFound();
            }

            return View(solicitud);
        }
    }
}
