using Gastito.Entities.Enums;

namespace Gastito.Entities.Models;

public class Tarjeta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoTarjeta TipoTarjeta { get; set; }

    // Navigation property
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
