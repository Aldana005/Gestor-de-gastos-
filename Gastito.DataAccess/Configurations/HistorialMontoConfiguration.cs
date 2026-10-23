using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastito.DataAccess.Configurations;

public class HistorialMontoConfiguration : IEntityTypeConfiguration<HistorialMonto>
{
    public void Configure(EntityTypeBuilder<HistorialMonto> builder)
    {
        builder.ToTable("HistorialMontos");

        builder.HasKey(h => h.Id);

        // Filter out history for soft-deleted transactions
        builder.HasQueryFilter(h => !h.Transaction!.IsDeleted);

        builder.Property(h => h.Monto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(h => h.VigenteDesde)
            .IsRequired();

        builder.Property(h => h.CreatedAtUtc)
            .IsRequired();
    }
}
