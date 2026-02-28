using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Domain.Entities;

public class CestaRecomendacao
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataDesativacao { get; set; }

    // Navigation
    public ICollection<CestaItem> Itens { get; set; } = new List<CestaItem>();
}

public class CestaItem
{
    public int Id { get; set; }

    public int CestaRecomendacaoId { get; set; }

    [Required, MaxLength(12)]
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Percentual de alocação na cesta (ex: 30.00 para 30%).
    /// A soma de todos os itens deve ser exatamente 100%.
    /// </summary>
    public decimal Percentual { get; set; }

    // Navigation
    public CestaRecomendacao? CestaRecomendacao { get; set; }
}
