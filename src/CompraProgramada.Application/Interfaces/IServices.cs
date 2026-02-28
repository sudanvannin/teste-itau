using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces;

/// <summary>
/// Serviço de gerenciamento de clientes — adesão, saída, alterações e consultas.
/// </summary>
public interface IClienteService
{
    Task<AdesaoResponse> AderirAsync(AdesaoRequest request);
    Task<SaidaResponse> SairAsync(int clienteId);
    Task<AlterarValorMensalResponse> AlterarValorMensalAsync(int clienteId, AlterarValorMensalRequest request);
    Task<CarteiraResponse> ConsultarCarteiraAsync(int clienteId);
    Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(int clienteId);
}

/// <summary>
/// Gerenciamento da cesta de recomendação (Top Five).
/// </summary>
public interface ICestaService
{
    Task<CestaResponse> CriarOuSubstituirAsync(CriarCestaRequest request);
    Task<CestaResponse> ObterAtivaAsync();
}

/// <summary>
/// Serviço de cotações — busca preços de fechamento dos arquivos COTAHIST da B3.
/// </summary>
public interface ICotacaoService
{
    decimal? ObterPrecoFechamento(string ticker);
    Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers);
}

/// <summary>
/// Motor de compra consolidada na conta Master.
/// </summary>
public interface IMotorCompraService
{
    Task<CompraConsolidadaResponse> ExecutarCompraAsync(DisparoCompraRequest request);
}

/// <summary>
/// Distribuição de ativos da Master para Filhotes.
/// </summary>
public interface IDistribuicaoService
{
    Task<DistribuicaoResponse> DistribuirAsync(int ordemCompraId);
}

/// <summary>
/// Rebalanceamento de carteiras conforme nova cesta.
/// </summary>
public interface IRebalanceamentoService
{
    Task<RebalanceamentoResponse> RebalancearAsync();
}
