using CompraProgramada.Application.Interfaces;

namespace CompraProgramada.Infrastructure.Cotacoes;

/// <summary>
/// Implementação do serviço de cotações usando arquivos COTAHIST da B3.
/// Procura o arquivo mais recente na pasta cotacoes/ e retorna a cotação do ticker solicitado.
/// </summary>
public class CotacaoServiceImpl : ICotacaoService
{
    private readonly CotahistParser _parser;
    private readonly string _pastaCotacoes;

    public CotacaoServiceImpl(string pastaCotacoes)
    {
        _parser = new CotahistParser();
        _pastaCotacoes = pastaCotacoes;
    }

    /// <summary>
    /// Obtém o preço de fechamento mais recente de um ticker.
    /// Busca nos arquivos COTAHIST da pasta cotacoes/ em ordem decrescente de data.
    /// Filtra apenas mercado à vista (TPMERC = 010, CODBDI = 02).
    /// </summary>
    public decimal? ObterPrecoFechamento(string ticker)
    {
        if (!Directory.Exists(_pastaCotacoes))
            return null;

        var arquivos = Directory.GetFiles(_pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var cotacoes = _parser.ParseArquivo(arquivo);
            var cotacao = cotacoes
                .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.TipoMercado == 10) // Mercado à vista
                .Where(c => c.CodigoBDI == "02")  // Lote padrão
                .FirstOrDefault();

            if (cotacao != null)
                return cotacao.PrecoFechamento;
        }

        return null;
    }

    /// <summary>
    /// Obtém preços de fechamento de múltiplos tickers de uma só vez.
    /// Evita re-parsear o mesmo arquivo múltiplas vezes.
    /// </summary>
    public Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers)
    {
        var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var tickersSet = new HashSet<string>(tickers, StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_pastaCotacoes))
            return resultado;

        var arquivos = Directory.GetFiles(_pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var cotacoes = _parser.ParseArquivo(arquivo);

            foreach (var cotacao in cotacoes
                .Where(c => c.TipoMercado == 10 && c.CodigoBDI == "02")
                .Where(c => tickersSet.Contains(c.Ticker)))
            {
                if (!resultado.ContainsKey(cotacao.Ticker))
                    resultado[cotacao.Ticker] = cotacao.PrecoFechamento;
            }

            // Se já encontrou todos, não precisa ler mais arquivos
            if (resultado.Count == tickersSet.Count)
                break;
        }

        return resultado;
    }
}
