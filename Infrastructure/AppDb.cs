using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class AppDb : IdentityDbContext<IdentityUser>
    {
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Equipo> Equipos => Set<Equipo>();
        public DbSet<Label> Labels => Set<Label>();
        public DbSet<HistorialLabel> Historial => Set<HistorialLabel>();
        public DbSet<SuscripcionAlerta> Suscripciones => Set<SuscripcionAlerta>();

        public AppDb(DbContextOptions<AppDb> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<Area>(e =>
            {
                e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
                e.HasIndex(x => x.Nombre).IsUnique();
            });

            mb.Entity<Equipo>(e =>
            {
                e.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Codigo).IsUnique();
                e.HasOne(x => x.Area).WithMany(a => a.Equipos).HasForeignKey(x => x.AreaId);
            });

            mb.Entity<Label>(l =>
            {
                l.HasOne(x => x.Equipo).WithMany(e => e.Labels).HasForeignKey(x => x.EquipoId)
                 .OnDelete(DeleteBehavior.Cascade);

                l.Property(x => x.CreadoPor).HasMaxLength(120).IsRequired();
                l.Property(x => x.Observacion).HasMaxLength(1000);
                l.Property(x => x.FotoUrl).HasMaxLength(500);
                l.Property(x => x.FotoContentType).HasMaxLength(64);

                l.HasIndex(x => x.FechaVencimiento);
            });

            mb.Entity<HistorialLabel>(h =>
            {
                h.HasOne(x => x.Label).WithMany(l => l.Historial).HasForeignKey(x => x.LabelId);
            });
        }
    }
}
