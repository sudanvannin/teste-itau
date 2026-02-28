using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Domain.Entities;

public class Distribuicao
{
    public int Id { get; set; }

    public int OrdemCompraId { get; set; }

    public int ClienteId { get; set; }

    public DateTime DataDistribuicao { get; set; } = DateTime.UtcNow;

    // Navigation
    public OrdemCompra? OrdemCompra { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<DistribuicaoItem> Itens { get; set; } = new List<DistribuicaoItem>();
}

public class DistribuicaoItem
{
    public int Id { get; set; }

    public int DistribuicaoId { get; set; }

    [Required, MaxLength(12)]
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    // Navigation
    public Distribuicao? Distribuicao { get; set; }
}
