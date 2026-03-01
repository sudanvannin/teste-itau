namespace CompraProgramada.Application.Services;

/// <summary>
/// Implementação sandbox do CotacaoService.
/// Retorna preços simulados para os ativos mais comuns da B3.
/// Em produção, substituir pela leitura real do arquivo COTAHIST.
/// </summary>
public class CotacaoService : Interfaces.ICotacaoService
{
    // Preços de fechamento simulados (ref. fev/2026)
    private static readonly Dictionary<string, decimal> _precos = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PETR4",  35.52m  },
        { "PETR3",  38.10m  },
        { "VALE3",  66.80m  },
        { "ITUB4",  32.15m  },
        { "ITUB3",  30.40m  },
        { "BBDC4",  15.90m  },
        { "BBDC3",  14.70m  },
        { "BBAS3",  55.70m  },
        { "ABEV3",  13.20m  },
        { "MGLU3",   2.80m  },
        { "WEGE3",  45.30m  },
        { "RENT3",  65.50m  },
        { "LREN3",  15.80m  },
        { "RADL3",  50.10m  },
        { "PRIO3",  44.90m  },
        // Lotes fracionários (ticker + F) — mesmo preço
        { "PETR4F", 35.52m  },
        { "VALE3F",  66.80m  },
        { "ITUB4F", 32.15m  },
        { "BBDC4F", 15.90m  },
        { "BBAS3F", 55.70m  },
    };

    public decimal? ObterPrecoFechamento(string ticker)
        => _precos.TryGetValue(ticker, out var p) ? p : null;

    public Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers)
        => tickers
            .Where(t => _precos.ContainsKey(t))
            .ToDictionary(t => t, t => _precos[t], StringComparer.OrdinalIgnoreCase);
}
