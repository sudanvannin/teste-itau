using System.ComponentModel.DataAnnotations;
using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class OrdemCompra
{
    public int Id { get; set; }

    public DateTime DataExecucao { get; set; }

    /// <summary>
    /// Valor total consolidado de todos os aportes dos clientes para esta data.
    /// </summary>
    public decimal TotalConsolidado { get; set; }

    public StatusOrdem Status { get; set; } = StatusOrdem.Pendente;

    // Navigation
    public ICollection<OrdemCompraItem> Itens { get; set; } = new List<OrdemCompraItem>();
    public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();
}

public class OrdemCompraItem
{
    public int Id { get; set; }

    public int OrdemCompraId { get; set; }

    [Required, MaxLength(12)]
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public TipoMercado TipoMercado { get; set; }

    public decimal PrecoUnitario { get; set; }

    // Navigation
    public OrdemCompra? OrdemCompra { get; set; }
}
