using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastito.DataAccess.Configurations;

public class PagoCuotaConfiguration : IEntityTypeConfiguration<PagoCuota>
{
    public void Configure(EntityTypeBuilder<PagoCuota> builder)
    {
        builder.ToTable("PagosCuota");

        builder.HasKey(p => p.Id);

        // Filter out quotas for soft-deleted transactions
        builder.HasQueryFilter(p => !p.Transaction!.IsDeleted);

        builder.Property(p => p.CantidadCuotas)
            .IsRequired();

        builder.Property(p => p.Monto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.CuotaDesde)
            .IsRequired();

        builder.Property(p => p.CuotaHasta)
            .IsRequired();

        builder.Property(p => p.FechaPagoUtc)
            .IsRequired();
    }
}
