using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Domain.Entities;

public class CustodiaItem
{
    public int Id { get; set; }

    public int ContaGraficaId { get; set; }

    [Required, MaxLength(12)]
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    /// <summary>
    /// Preço médio de aquisição. Recalculado a cada compra.
    /// Vendas NÃO alteram o preço médio (RN-043).
    /// </summary>
    public decimal PrecoMedio { get; set; }

    // Navigation
    public ContaGrafica? ContaGrafica { get; set; }
}
