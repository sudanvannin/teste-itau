using CompraProgramada.Domain.Services;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Domain;

public class PrecoMedioCalculatorTests
{
    [Fact]
    public void Calcular_PrimeiraCompra_DeveRetornarPrecoCompra()
    {
        var resultado = PrecoMedioCalculator.Calcular(0, 0m, 100, 35.00m);
        resultado.Should().Be(35.00m);
    }

    [Fact]
    public void Calcular_SegundaCompra_DeveRetornarMediaPonderada()
    {
        // 100 ações a R$35 + 50 ações a R$38 = PM R$36.00
        var resultado = PrecoMedioCalculator.Calcular(100, 35.00m, 50, 38.00m);
        resultado.Should().Be(36.00m);
    }

    [Fact]
    public void Calcular_TerceiraCompra_DeveConsiderarPMAnterior()
    {
        // Cenário do desafio: 8×35 + 10×37 = PM 36.11
        var resultado = PrecoMedioCalculator.Calcular(8, 35.00m, 10, 37.00m);
        resultado.Should().Be(36.11m);
    }

    [Fact]
    public void Calcular_AposVenda_RecalculaSomenteComNovaCompra()
    {
        // 13 ações com PM 36.11 + 7 ações a 38.00 → PM 36.77
        var resultado = PrecoMedioCalculator.Calcular(13, 36.11m, 7, 38.00m);
        resultado.Should().Be(36.77m);
    }

    [Fact]
    public void Calcular_QuantidadeNovaNegativa_DeveLancarExcecao()
    {
        var act = () => PrecoMedioCalculator.Calcular(100, 35m, -10, 38m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calcular_QuantidadeAnteriorNegativa_DeveLancarExcecao()
    {
        var act = () => PrecoMedioCalculator.Calcular(-1, 35m, 10, 38m);
        act.Should().Throw<ArgumentException>();
    }
}
