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

public interface ICestaService { }

/// <summary>
/// Serviço de cotações — busca preços de fechamento dos arquivos COTAHIST da B3.
/// </summary>
public interface ICotacaoService
{
    decimal? ObterPrecoFechamento(string ticker);
    Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers);
}

public interface IMotorCompraService { }
public interface IDistribuicaoService { }
public interface IRebalanceamentoService { }
