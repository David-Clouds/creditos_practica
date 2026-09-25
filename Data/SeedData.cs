using CreditosPlataforma.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CreditosPlataforma.Web.Data
{
    public static class SeedData
    {
        public static async Task InicializarAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Analista"))
            {
                await roleManager.CreateAsync(new IdentityRole("Analista"));
            }

            var analista = await userManager.FindByEmailAsync("analista@creditos.com");
            if (analista == null)
            {
                analista = new IdentityUser
                {
                    UserName = "analista@creditos.com",
                    Email = "analista@creditos.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(analista, "Analista123!");
                await userManager.AddToRoleAsync(analista, "Analista");
            }

            if (!context.Clientes.Any())
            {
                var usuarioCliente1 = await CrearUsuarioSiNoExiste(userManager, "cliente1@creditos.com", "Cliente123!");
                var usuarioCliente2 = await CrearUsuarioSiNoExiste(userManager, "cliente2@creditos.com", "Cliente123!");

                var cliente1 = new Cliente { UsuarioId = usuarioCliente1.Id, IngresosMensuales = 3000, Activo = true };
                var cliente2 = new Cliente { UsuarioId = usuarioCliente2.Id, IngresosMensuales = 5000, Activo = true };

                context.Clientes.AddRange(cliente1, cliente2);
                await context.SaveChangesAsync();

                context.SolicitudesCredito.AddRange(
                    new SolicitudCredito
                    {
                        ClienteId = cliente1.Id,
                        MontoSolicitado = 8000,
                        FechaSolicitud = DateTime.UtcNow,
                        Estado = EstadoSolicitud.Pendiente
                    },
                    new SolicitudCredito
                    {
                        ClienteId = cliente2.Id,
                        MontoSolicitado = 10000,
                        FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                        Estado = EstadoSolicitud.Aprobado
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task<IdentityUser> CrearUsuarioSiNoExiste(UserManager<IdentityUser> userManager, string email, string password)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await userManager.CreateAsync(usuario, password);
            }
            return usuario;
        }
    }
}