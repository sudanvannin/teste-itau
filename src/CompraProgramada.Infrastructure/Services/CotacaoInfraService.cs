using CompraProgramada.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Services;

/// <summary>
/// Implementação real do ICotacaoService que lê arquivos COTAHIST da B3.
/// Usa o arquivo mais recente da pasta cotacoes/ definida em appsettings.json.
/// Faz fallback para preços sandbox se a pasta estiver vazia ou o ticker não for encontrado.
/// </summary>
public class CotacaoInfraService : ICotacaoService
{
    private readonly string _pastaCotacoes;
    private readonly ILogger<CotacaoInfraService> _logger;

    // Cache em memória para evitar re-leitura do arquivo a cada request
    private Dictionary<string, decimal>? _cache;
    private string? _arquivoCarregado;

    // Preços sandbox (fallback quando não há arquivo COTAHIST disponível)
    private static readonly Dictionary<string, decimal> _precosSandbox = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PETR4",  35.52m }, { "PETR4F", 35.52m },
        { "VALE3",  66.80m }, { "VALE3F",  66.80m },
        { "ITUB4",  32.15m }, { "ITUB4F", 32.15m },
        { "BBDC4",  15.90m }, { "BBDC4F", 15.90m },
        { "BBAS3",  55.70m }, { "BBAS3F", 55.70m },
        { "ABEV3",  13.20m }, { "WEGE3",  45.30m },
        { "RENT3",  65.50m }, { "LREN3",  15.80m },
        { "MGLU3",   2.80m }, { "PRIO3",  44.90m },
    };

    public CotacaoInfraService(IConfiguration config, ILogger<CotacaoInfraService> logger)
    {
        _pastaCotacoes = config["Cotacoes:PastaCotacoes"] ?? "cotacoes";
        _logger = logger;
    }

    public decimal? ObterPrecoFechamento(string ticker)
    {
        var cotacoes = CarregarCotacoes();
        return cotacoes.TryGetValue(ticker, out var p) ? p : null;
    }

    public Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers)
    {
        var cotacoes = CarregarCotacoes();
        return tickers
            .Where(t => cotacoes.ContainsKey(t))
            .ToDictionary(t => t, t => cotacoes[t], StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, decimal> CarregarCotacoes()
    {
        // Encontrar o arquivo COTAHIST mais recente
        var arquivoMaisRecente = EncontrarArquivoMaisRecente();

        if (arquivoMaisRecente == null)
        {
            _logger.LogWarning(
                "Nenhum arquivo COTAHIST encontrado em '{Pasta}'. Usando preços sandbox.",
                _pastaCotacoes);
            return _precosSandbox;
        }

        // Usar cache se o arquivo não mudou
        if (_cache != null && _arquivoCarregado == arquivoMaisRecente)
            return _cache;

        _logger.LogInformation("Carregando cotações do arquivo: {Arquivo}", arquivoMaisRecente);

        try
        {
            _cache = ParsearCotahist(arquivoMaisRecente);
            _arquivoCarregado = arquivoMaisRecente;

            _logger.LogInformation(
                "Cotações carregadas: {Total} tickers do arquivo {Arquivo}",
                _cache.Count, Path.GetFileName(arquivoMaisRecente));

            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parsear {Arquivo}. Usando preços sandbox.", arquivoMaisRecente);
            return _precosSandbox;
        }
    }

    private string? EncontrarArquivoMaisRecente()
    {
        if (!Directory.Exists(_pastaCotacoes))
            return null;

        return Directory
            .GetFiles(_pastaCotacoes, "COTAHIST_*.TXT", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f) // nome do arquivo contém data (ex: COTAHIST_D20260225.TXT)
            .FirstOrDefault();
    }

    /// <summary>
    /// Parser de campos fixos do arquivo COTAHIST da B3.
    /// Layout: 245 chars por linha — tipo 01 = detalhe de cotação.
    /// CODNEG: pos 13-24 | PREULT: pos 109-121 (preço × 100, precisão N(11,2))
    /// </summary>
    private static Dictionary<string, decimal> ParsearCotahist(string caminhoArquivo)
    {
        var cotacoes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        // COTAHIST usa encoding Latin-1 (ISO-8859-1)
        var encoding = System.Text.Encoding.Latin1;

        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            if (linha.Length < 121) continue;

            // Tipo de registro: posição 1-2 (0-indexed: 0-1)
            var tipoReg = linha.AsSpan(0, 2);
            if (!tipoReg.SequenceEqual("01")) continue;

            // Ticker: posições 13-24 (0-indexed: 12-23), trimado
            var ticker = linha.AsSpan(12, 12).ToString().Trim();
            if (string.IsNullOrWhiteSpace(ticker)) continue;

            // Preço de fechamento: posições 109-121 (0-indexed: 108-120) — 13 chars, escala ×100
            var precoStr = linha.AsSpan(108, 13).ToString().Trim();
            if (!decimal.TryParse(precoStr, out var precoRaw)) continue;

            // B3 armazena sem ponto decimal (ex: "000003552" = R$35,52)
            var preco = precoRaw / 100m;
            if (preco <= 0) continue;

            // Guarda o último preço encontrado para o ticker (arquivo é ordenado por data)
            cotacoes[ticker] = preco;
        }

        return cotacoes;
    }
}
