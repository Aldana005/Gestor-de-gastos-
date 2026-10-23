using Gastito.Entities.Enums;

namespace Gastito.Entities.Models;

public class Category
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimiento Tipo { get; set; }
    public bool EsSistema { get; set; }

    // Navigation property
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
