using CompraProgramada.Domain.Services;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Domain;

public class IRCalculatorTests
{
    // ── IR Dedo-Duro ──────────────────────────────────────────

    [Fact]
    public void CalcularDedoDuro_OperacaoNormal_DeveRetornar0005Porcento()
    {
        // R$ 280,00 × 0,005% = R$ 0,01
        var resultado = IRCalculator.CalcularDedoDuro(280.00m);
        resultado.Should().Be(0.01m);
    }

    [Fact]
    public void CalcularDedoDuro_OperacaoMaior_DeveCalcularCorretamente()
    {
        // R$ 10.000 × 0,005% = R$ 0,50
        var resultado = IRCalculator.CalcularDedoDuro(10_000m);
        resultado.Should().Be(0.50m);
    }

    [Fact]
    public void CalcularDedoDuro_ValorZero_DeveRetornarZero()
    {
        IRCalculator.CalcularDedoDuro(0m).Should().Be(0m);
    }

    [Fact]
    public void CalcularDedoDuro_ValorNegativo_DeveLancarExcecao()
    {
        var act = () => IRCalculator.CalcularDedoDuro(-100m);
        act.Should().Throw<ArgumentException>();
    }

    // ── IR Venda (Rebalanceamento) ────────────────────────────

    [Fact]
    public void CalcularIRVenda_VendasAbaixoLimite_DeveSerIsento()
    {
        // Total vendas R$ 230 < R$ 20.000 → ISENTO
        var resultado = IRCalculator.CalcularIRVenda(230m, 50m);
        resultado.Should().Be(0m);
    }

    [Fact]
    public void CalcularIRVenda_VendasExatamenteNoLimite_DeveSerIsento()
    {
        var resultado = IRCalculator.CalcularIRVenda(20_000m, 5_000m);
        resultado.Should().Be(0m);
    }

    [Fact]
    public void CalcularIRVenda_VendasAcimaLimite_ComLucro_DeveTributar20Porcento()
    {
        // Total vendas R$ 21.500 > R$ 20.000, lucro R$ 3.100 → IR = R$ 620
        var resultado = IRCalculator.CalcularIRVenda(21_500m, 3_100m);
        resultado.Should().Be(620m);
    }

    [Fact]
    public void CalcularIRVenda_VendasAcimaLimite_ComPrejuizo_DeveRetornarZero()
    {
        // Total vendas R$ 24.400 > R$ 20.000, prejuízo -R$ 600 → IR = R$ 0
        var resultado = IRCalculator.CalcularIRVenda(24_400m, -600m);
        resultado.Should().Be(0m);
    }

    // ── Lucro Líquido ─────────────────────────────────────────

    [Fact]
    public void CalcularLucroLiquido_ComLucro_DeveRetornarPositivo()
    {
        // 500 ações vendidas a R$16, PM R$14 → Lucro R$1.000
        var resultado = IRCalculator.CalcularLucroLiquido(500, 16m, 14m);
        resultado.Should().Be(1_000m);
    }

    [Fact]
    public void CalcularLucroLiquido_ComPrejuizo_DeveRetornarNegativo()
    {
        // 400 ações vendidas a R$32, PM R$35 → Prejuízo -R$1.200
        var resultado = IRCalculator.CalcularLucroLiquido(400, 32m, 35m);
        resultado.Should().Be(-1_200m);
    }

    [Fact]
    public void CalcularLucroLiquido_VendaNoPrecoMedio_DeveRetornarZero()
    {
        var resultado = IRCalculator.CalcularLucroLiquido(100, 35m, 35m);
        resultado.Should().Be(0m);
    }
}
