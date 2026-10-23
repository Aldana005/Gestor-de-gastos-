using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Gastito.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tarjeta> Tarjetas => Set<Tarjeta>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<HistorialMonto> HistorialMontos => Set<HistorialMonto>();
    public DbSet<PagoCuota> PagosCuota => Set<PagoCuota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations defined in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
