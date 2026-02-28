namespace CompraProgramada.Domain.Services;

/// <summary>
/// Calcula impostos de renda sobre operações de renda variável.
/// </summary>
public static class IRCalculator
{
    /// <summary>
    /// Alíquota do IR Dedo-Duro (IRRF): 0,005% sobre o valor da operação.
    /// </summary>
    private const decimal AliquotaDedoDuro = 0.00005m;

    /// <summary>
    /// Alíquota de IR sobre lucro em vendas quando total mensal > R$ 20.000.
    /// </summary>
    private const decimal AliquotaIRVenda = 0.20m;

    /// <summary>
    /// Limite mensal de vendas para isenção de IR para pessoa física.
    /// </summary>
    private const decimal LimiteIsencaoMensal = 20_000m;

    /// <summary>
    /// Calcula o IR Dedo-Duro (0,005% sobre o valor da operação).
    /// RN-053 a RN-055.
    /// </summary>
    public static decimal CalcularDedoDuro(decimal valorOperacao)
    {
        if (valorOperacao < 0)
            throw new ArgumentException("Valor da operação não pode ser negativo.", nameof(valorOperacao));

        return Math.Round(valorOperacao * AliquotaDedoDuro, 2);
    }

    /// <summary>
    /// Calcula o IR sobre vendas em rebalanceamento.
    /// RN-057 a RN-062.
    /// Se total de vendas no mês <= R$ 20.000: ISENTO.
    /// Se > R$ 20.000: 20% sobre o lucro líquido total.
    /// Se prejuízo: R$ 0,00.
    /// </summary>
    public static decimal CalcularIRVenda(decimal totalVendasMes, decimal lucroLiquido)
    {
        if (totalVendasMes <= LimiteIsencaoMensal)
            return 0m;

        if (lucroLiquido <= 0)
            return 0m;

        return Math.Round(lucroLiquido * AliquotaIRVenda, 2);
    }

    /// <summary>
    /// Calcula o lucro líquido de uma venda.
    /// Lucro = Valor de Venda - (Quantidade × Preço Médio)
    /// </summary>
    public static decimal CalcularLucroLiquido(int quantidade, decimal precoVenda, decimal precoMedio)
    {
        var valorVenda = quantidade * precoVenda;
        var custoAquisicao = quantidade * precoMedio;
        return valorVenda - custoAquisicao;
    }
}
