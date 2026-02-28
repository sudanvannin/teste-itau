using CompraProgramada.Infrastructure.Cotacoes;
using FluentAssertions;

namespace CompraProgramada.UnitTests.Infrastructure;

public class CotahistParserTests
{
    private readonly CotahistParser _parser = new();
    private readonly string _testFilePath;

    public CotahistParserTests()
    {
        // O arquivo de teste está em TestData/ dentro do projeto de testes
        _testFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData", "COTAHIST_D20260225.TXT");
    }

    [Fact]
    public void ParseArquivo_DeveRetornarApenasRegistrosDeDetalhe()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // 5 ações mercado à vista + 1 fracionário = 6 registros
        // Header (00) e Trailer (99) são ignorados
        cotacoes.Should().HaveCount(6);
    }

    [Fact]
    public void ParseArquivo_DeveIgnorarHeaderETrailer()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        cotacoes.Should().NotContain(c => c.Ticker == "COTAHIST.2026");
    }

    [Fact]
    public void ParseArquivo_DeveExtrairTickerCorretamente()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        cotacoes.Select(c => c.Ticker).Should().Contain("PETR4", "VALE3", "ITUB4", "BBDC4", "WEGE3", "PETR4F");
    }

    [Fact]
    public void ParseArquivo_DeveConverterPrecoFechamentoCorretamente()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // PETR4: PREULT = 0000000003580 → R$ 35,80
        var petr4 = cotacoes.First(c => c.Ticker == "PETR4" && c.TipoMercado == 10);
        petr4.PrecoFechamento.Should().Be(35.80m);
    }

    [Fact]
    public void ParseArquivo_DeveConverterPrecoAbertura()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // PETR4: PREABE = 0000000003520 → R$ 35,20
        var petr4 = cotacoes.First(c => c.Ticker == "PETR4" && c.TipoMercado == 10);
        petr4.PrecoAbertura.Should().Be(35.20m);
    }

    [Fact]
    public void ParseArquivo_DeveConverterPrecoMaximoEMinimo()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        var petr4 = cotacoes.First(c => c.Ticker == "PETR4" && c.TipoMercado == 10);
        petr4.PrecoMaximo.Should().Be(36.50m);
        petr4.PrecoMinimo.Should().Be(34.80m);
    }

    [Fact]
    public void ParseArquivo_DeveExtrairDataPregaoCorretamente()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        var petr4 = cotacoes.First(c => c.Ticker == "PETR4");
        petr4.DataPregao.Should().Be(new DateTime(2026, 2, 25));
    }

    [Fact]
    public void ParseArquivo_DeveFiltrarPorTipoMercado()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // 5 registros mercado à vista (010) + 1 fracionário (020)
        cotacoes.Where(c => c.TipoMercado == 10).Should().HaveCount(5);
        cotacoes.Where(c => c.TipoMercado == 20).Should().HaveCount(1);
    }

    [Fact]
    public void ParseArquivo_DeveExtrairCodigoBDI()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // Mercado à vista: BDI = 02 (lote padrão)
        cotacoes.Where(c => c.TipoMercado == 10)
            .Should().OnlyContain(c => c.CodigoBDI == "02");

        // Fracionário: BDI = 96
        cotacoes.Where(c => c.TipoMercado == 20)
            .Should().OnlyContain(c => c.CodigoBDI == "96");
    }

    [Fact]
    public void ParseArquivo_DeveExtrairMultiplosTickers()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        // VALE3: fechamento = R$ 62,00
        cotacoes.First(c => c.Ticker == "VALE3").PrecoFechamento.Should().Be(62.00m);

        // ITUB4: fechamento = R$ 30,00
        cotacoes.First(c => c.Ticker == "ITUB4").PrecoFechamento.Should().Be(30.00m);

        // BBDC4: fechamento = R$ 15,00
        cotacoes.First(c => c.Ticker == "BBDC4").PrecoFechamento.Should().Be(15.00m);

        // WEGE3: fechamento = R$ 40,00
        cotacoes.First(c => c.Ticker == "WEGE3").PrecoFechamento.Should().Be(40.00m);
    }

    [Fact]
    public void ParseArquivo_FracionarioDeveSerIdentificado()
    {
        var cotacoes = _parser.ParseArquivo(_testFilePath).ToList();

        var petr4F = cotacoes.First(c => c.Ticker == "PETR4F");
        petr4F.TipoMercado.Should().Be(20);
        petr4F.CodigoBDI.Should().Be("96");
        petr4F.PrecoFechamento.Should().Be(35.70m);
    }

    [Fact]
    public void ParseArquivo_ArquivoInexistente_DeveLancarExcecao()
    {
        var act = () => _parser.ParseArquivo("arquivo_inexistente.txt").ToList();
        act.Should().Throw<FileNotFoundException>();
    }
}
