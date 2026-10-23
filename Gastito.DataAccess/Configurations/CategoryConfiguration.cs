using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastito.DataAccess.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Tipo)
            .IsRequired();

        builder.Property(c => c.EsSistema)
            .IsRequired()
            .HasDefaultValue(false);

        // Seed de categorías iniciales del sistema (Sección 3.2)
        builder.HasData(
            new Category { Id = 1, Nombre = "Supermercado", Tipo = TipoMovimiento.Gasto, EsSistema = true },
            new Category { Id = 2, Nombre = "Hogar", Tipo = TipoMovimiento.Gasto, EsSistema = true },
            new Category { Id = 3, Nombre = "Transporte", Tipo = TipoMovimiento.Gasto, EsSistema = true },
            new Category { Id = 4, Nombre = "Ingreso Principal", Tipo = TipoMovimiento.Ingreso, EsSistema = true }
        );
    }
}
