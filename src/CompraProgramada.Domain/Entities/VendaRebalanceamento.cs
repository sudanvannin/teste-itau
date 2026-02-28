using System.ComponentModel.DataAnnotations;
using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

/// <summary>
/// Registra vendas realizadas durante rebalanceamentos.
/// Usado para calcular IR sobre vendas (RN-057 a RN-062).
/// </summary>
public class VendaRebalanceamento
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    [Required, MaxLength(12)]
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal PrecoVenda { get; set; }

    /// <summary>
    /// Preço médio de aquisição no momento da venda.
    /// Usado para calcular lucro/prejuízo.
    /// </summary>
    public decimal PrecoMedio { get; set; }

    public DateTime DataVenda { get; set; } = DateTime.UtcNow;

    // Navigation
    public Cliente? Cliente { get; set; }
}
