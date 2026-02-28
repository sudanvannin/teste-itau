using CompraProgramada.Domain.Services;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Domain;

public class LotePadraoSplitterTests
{
    [Theory]
    [InlineData(350, 300, 50)]
    [InlineData(100, 100, 0)]
    [InlineData(99, 0, 99)]
    [InlineData(0, 0, 0)]
    [InlineData(28, 0, 28)]
    [InlineData(200, 200, 0)]
    [InlineData(1050, 1000, 50)]
    public void Separar_DeveRetornarLoteEFracionarioCorretos(
        int total, int expectedLote, int expectedFracionario)
    {
        var (lote, frac) = LotePadraoSplitter.Separar(total);
        lote.Should().Be(expectedLote);
        frac.Should().Be(expectedFracionario);
    }

    [Fact]
    public void Separar_QuantidadeNegativa_DeveLancarExcecao()
    {
        var act = () => LotePadraoSplitter.Separar(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PETR4", "PETR4F")]
    [InlineData("VALE3", "VALE3F")]
    [InlineData("ITUB4", "ITUB4F")]
    public void TickerFracionario_DeveAdicionarSufixoF(string ticker, string expected)
    {
        LotePadraoSplitter.TickerFracionario(ticker).Should().Be(expected);
    }

    [Fact]
    public void TickerFracionario_TickerVazio_DeveLancarExcecao()
    {
        var act = () => LotePadraoSplitter.TickerFracionario("");
        act.Should().Throw<ArgumentException>();
    }
}
