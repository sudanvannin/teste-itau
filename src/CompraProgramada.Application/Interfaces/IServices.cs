namespace CompraProgramada.Application.Interfaces;

public interface IClienteService { }
public interface ICestaService { }

/// <summary>
/// Serviço de cotações — busca preços de fechamento dos arquivos COTAHIST da B3.
/// </summary>
public interface ICotacaoService
{
    /// <summary>
    /// Obtém o preço de fechamento mais recente de um ticker.
    /// Retorna null se o ticker não for encontrado.
    /// </summary>
    decimal? ObterPrecoFechamento(string ticker);

    /// <summary>
    /// Obtém preços de fechamento de múltiplos tickers em uma única operação.
    /// Retorna dicionário ticker → preço.
    /// </summary>
    Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers);
}

public interface IMotorCompraService { }
public interface IDistribuicaoService { }
public interface IRebalanceamentoService { }
