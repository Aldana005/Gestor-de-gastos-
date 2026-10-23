using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastito.DataAccess.Configurations;

public class TarjetaConfiguration : IEntityTypeConfiguration<Tarjeta>
{
    public void Configure(EntityTypeBuilder<Tarjeta> builder)
    {
        builder.ToTable("Tarjetas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TipoTarjeta)
            .IsRequired();
    }
}
