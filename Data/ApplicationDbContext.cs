using CreditosPlataforma.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CreditosPlataforma.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<SolicitudCredito> SolicitudesCredito { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Cliente>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_Cliente_IngresosMensuales", "IngresosMensuales > 0"));
            });

            builder.Entity<SolicitudCredito>(entity =>
            {
                entity.Property(s => s.Estado)
                      .HasConversion<string>();

                entity.ToTable(t => t.HasCheckConstraint("CK_SolicitudCredito_Monto", "MontoSolicitado > 0"));

                // Restricción: solo una solicitud Pendiente por cliente
                entity.HasIndex(s => s.ClienteId)
                      .IsUnique()
                      .HasFilter("Estado = 'Pendiente'")
                      .HasDatabaseName("IX_SolicitudCredito_UnaPendientePorCliente");

                entity.HasOne(s => s.Cliente)
                      .WithMany(c => c.Solicitudes)
                      .HasForeignKey(s => s.ClienteId);
            });
        }
    }
}