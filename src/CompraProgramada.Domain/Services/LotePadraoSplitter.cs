namespace CompraProgramada.Domain.Services;

/// <summary>
/// Separa a quantidade total de ações em lote padrão (múltiplos de 100) 
/// e mercado fracionário (1-99).
/// RN-031 a RN-033.
/// </summary>
public static class LotePadraoSplitter
{
    public const int TamanhoLote = 100;
    public const string SufixoFracionario = "F";

    /// <summary>
    /// Separa a quantidade em lote padrão e fracionário.
    /// </summary>
    /// <returns>Tupla (lotePadrao, fracionario)</returns>
    public static (int LotePadrao, int Fracionario) Separar(int quantidadeTotal)
    {
        if (quantidadeTotal < 0)
            throw new ArgumentException("Quantidade não pode ser negativa.", nameof(quantidadeTotal));

        var lotePadrao = (quantidadeTotal / TamanhoLote) * TamanhoLote;
        var fracionario = quantidadeTotal % TamanhoLote;

        return (lotePadrao, fracionario);
    }

    /// <summary>
    /// Retorna o ticker do mercado fracionário (adiciona sufixo "F").
    /// Ex: PETR4 → PETR4F
    /// </summary>
    public static string TickerFracionario(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker não pode ser vazio.", nameof(ticker));

        return ticker.TrimEnd() + SufixoFracionario;
    }
}
