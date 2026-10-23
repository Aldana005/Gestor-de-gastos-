namespace Gastito.Presentation.Outputs.Transactions;

public class HistorialMontoOutput
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime VigenteDesde { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
