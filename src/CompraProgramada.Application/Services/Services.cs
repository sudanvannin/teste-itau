namespace CompraProgramada.Application.Services;

// No stubs — all service implementations are in Infrastructure.Services
// This file kept for backward compatibility with any Application.Services imports
public class CotacaoService : Interfaces.ICotacaoService
{
    public decimal? ObterPrecoFechamento(string ticker) => null;
    public Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers) => new();
}
