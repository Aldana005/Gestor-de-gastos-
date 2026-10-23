namespace Gastito.Entities.Models;

public class HistorialMonto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public decimal Monto { get; set; }
    public DateTime VigenteDesde { get; set; } // UTC 00:00:00 del primer día del mes
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
