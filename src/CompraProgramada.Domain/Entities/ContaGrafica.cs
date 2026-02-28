using System.ComponentModel.DataAnnotations;
using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class ContaGrafica
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string NumeroConta { get; set; } = string.Empty;

    public TipoConta Tipo { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // FK — null para conta Master (não pertence a nenhum cliente)
    public int? ClienteId { get; set; }

    // Navigation
    public Cliente? Cliente { get; set; }
    public ICollection<CustodiaItem> Custodia { get; set; } = new List<CustodiaItem>();
}
