namespace CompraProgramada.Domain.Services;

/// <summary>
/// Calcula o preço médio ponderado de aquisição de ativos.
/// PM = (QtdAnterior × PMAnterior + QtdNova × PrecoNovo) / (QtdAnterior + QtdNova)
/// Vendas NÃO alteram o preço médio (RN-043).
/// </summary>
public static class PrecoMedioCalculator
{
    public static decimal Calcular(
        int quantidadeAnterior,
        decimal precoMedioAnterior,
        int quantidadeNova,
        decimal precoNovaCompra)
    {
        if (quantidadeAnterior < 0)
            throw new ArgumentException("Quantidade anterior não pode ser negativa.", nameof(quantidadeAnterior));

        if (quantidadeNova <= 0)
            throw new ArgumentException("Quantidade nova deve ser positiva.", nameof(quantidadeNova));

        var totalAnterior = quantidadeAnterior * precoMedioAnterior;
        var totalNovo = quantidadeNova * precoNovaCompra;
        var quantidadeTotal = quantidadeAnterior + quantidadeNova;

        return Math.Round((totalAnterior + totalNovo) / quantidadeTotal, 2);
    }
}
