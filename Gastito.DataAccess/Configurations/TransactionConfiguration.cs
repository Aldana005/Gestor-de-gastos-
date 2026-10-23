using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastito.DataAccess.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        // Global Query Filter para Soft Delete (Sección 3.8 y 7)
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.Property(t => t.Tipo)
            .IsRequired();

        builder.Property(t => t.Frecuencia)
            .IsRequired();

        builder.Property(t => t.Monto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.Moneda)
            .IsRequired()
            .HasDefaultValue(Moneda.ARS)
            .HasSentinel(Moneda.ARS);

        builder.Property(t => t.Fecha)
            .IsRequired();

        builder.Property(t => t.Descripcion)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(t => t.Notas)
            .HasMaxLength(1000);

        builder.Property(t => t.MontoTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relaciones
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Tarjeta)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.TarjetaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.HistorialMontos)
            .WithOne(h => h.Transaction)
            .HasForeignKey(h => h.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.PagosCuota)
            .WithOne(p => p.Transaction)
            .HasForeignKey(p => p.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
