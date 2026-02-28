using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(11)]
    public string CPF { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public decimal ValorMensal { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataAdesao { get; set; } = DateTime.UtcNow;

    public DateTime? DataSaida { get; set; }

    // Navigation
    public ContaGrafica? ContaGrafica { get; set; }
    public ICollection<HistoricoValorMensal> HistoricoValores { get; set; } = new List<HistoricoValorMensal>();
}
