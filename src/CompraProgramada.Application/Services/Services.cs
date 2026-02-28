namespace CompraProgramada.Application.Services;

public class ClienteService : Interfaces.IClienteService { }
public class CestaService : Interfaces.ICestaService { }

public class CotacaoService : Interfaces.ICotacaoService
{
    public decimal? ObterPrecoFechamento(string ticker) => null;
    public Dictionary<string, decimal> ObterCotacoesFechamento(IEnumerable<string> tickers) => new();
}

public class MotorCompraService : Interfaces.IMotorCompraService { }
public class DistribuicaoService : Interfaces.IDistribuicaoService { }
public class RebalanceamentoService : Interfaces.IRebalanceamentoService { }
