using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CompraProgramada.Infrastructure.Services;

public class RebalanceamentoService : IRebalanceamentoService
{
    private readonly AppDbContext _db;
    private readonly ICotacaoService _cotacaoService;
    private readonly IKafkaProducerService _kafka;
    private readonly KafkaSettings _kafkaSettings;

    public RebalanceamentoService(
        AppDbContext db,
        ICotacaoService cotacaoService,
        IKafkaProducerService kafka,
        IOptions<KafkaSettings> kafkaSettings)
    {
        _db = db;
        _cotacaoService = cotacaoService;
        _kafka = kafka;
        _kafkaSettings = kafkaSettings.Value;
    }

    /// <summary>
    /// Rebalanceia todos os clientes conforme a nova cesta (RN-045 a RN-052).
    /// 1. Calcula valor da carteira de cada cliente
    /// 2. Determina quantidades ideais por ticker
    /// 3. Vende excedentes e compra faltantes
    /// 4. Calcula IR sobre vendas (RN-057 a RN-062)
    /// </summary>
    public async Task<RebalanceamentoResponse> RebalancearAsync()
    {
        var cestaAtiva = await _db.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa)
            ?? throw new BusinessException("Nenhuma cesta ativa para rebalancear.", "CESTA_NAO_ENCONTRADA");

        var clientesAtivos = await _db.Clientes
            .Where(c => c.Ativo)
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg!.Custodia)
            .ToListAsync();

        var tickers = cestaAtiva.Itens.Select(i => i.Ticker).ToList();
        var todosTickersCustodia = clientesAtivos
            .SelectMany(c => c.ContaGrafica?.Custodia?.Select(ci => ci.Ticker) ?? Enumerable.Empty<string>())
            .Distinct();
        var todosTickers = tickers.Union(todosTickersCustodia).Distinct();
        var cotacoes = _cotacaoService.ObterCotacoesFechamento(todosTickers);

        var operacoesPorCliente = new List<RebalanceamentoClienteResponse>();

        foreach (var cliente in clientesAtivos)
        {
            var custodia = cliente.ContaGrafica?.Custodia?.ToList() ?? new List<CustodiaItem>();

            // Valor total da carteira
            var valorCarteira = custodia.Sum(ci =>
                ci.Quantidade * cotacoes.GetValueOrDefault(ci.Ticker, ci.PrecoMedio));

            if (valorCarteira <= 0) continue;

            var vendas = new List<OperacaoRebalanceamentoResponse>();
            var compras = new List<OperacaoRebalanceamentoResponse>();
            decimal totalVendasMes = 0m;
            decimal lucroLiquidoTotal = 0m;

            // Vender ativos que não estão na nova cesta
            foreach (var item in custodia.ToList())
            {
                var naNewCesta = cestaAtiva.Itens.Any(ci => ci.Ticker == item.Ticker);
                if (!naNewCesta && item.Quantidade > 0)
                {
                    var precoVenda = cotacoes.GetValueOrDefault(item.Ticker, item.PrecoMedio);
                    var valorVenda = item.Quantidade * precoVenda;
                    totalVendasMes += valorVenda;

                    var lucro = IRCalculator.CalcularLucroLiquido(
                        item.Quantidade, precoVenda, item.PrecoMedio);
                    lucroLiquidoTotal += lucro;

                    // Registrar venda
                    _db.VendasRebalanceamento.Add(new VendaRebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Ticker = item.Ticker,
                        Quantidade = item.Quantidade,
                        PrecoVenda = precoVenda,
                        PrecoMedio = item.PrecoMedio,
                        DataVenda = DateTime.UtcNow
                    });

                    vendas.Add(new OperacaoRebalanceamentoResponse(
                        item.Ticker, item.Quantidade, precoVenda));

                    item.Quantidade = 0;
                }
            }

            // Calcular alocação ideal e comprar faltantes
            foreach (var cestaItem in cestaAtiva.Itens)
            {
                var valorIdeal = valorCarteira * (cestaItem.Percentual / 100m);
                var preco = cotacoes.GetValueOrDefault(cestaItem.Ticker, 0m);
                if (preco <= 0) continue;

                var qtdIdeal = (int)(valorIdeal / preco);
                var custodiaTicker = custodia.FirstOrDefault(ci => ci.Ticker == cestaItem.Ticker);
                var qtdAtual = custodiaTicker?.Quantidade ?? 0;
                var diferenca = qtdIdeal - qtdAtual;

                if (diferenca > 0)
                {
                    // Comprar
                    if (custodiaTicker != null)
                    {
                        custodiaTicker.PrecoMedio = PrecoMedioCalculator.Calcular(
                            custodiaTicker.Quantidade, custodiaTicker.PrecoMedio,
                            diferenca, preco);
                        custodiaTicker.Quantidade += diferenca;
                    }
                    else
                    {
                        cliente.ContaGrafica?.Custodia?.Add(new CustodiaItem
                        {
                            ContaGraficaId = cliente.ContaGrafica.Id,
                            Ticker = cestaItem.Ticker,
                            Quantidade = diferenca,
                            PrecoMedio = preco
                        });
                    }

                    compras.Add(new OperacaoRebalanceamentoResponse(
                        cestaItem.Ticker, diferenca, preco));
                }
                else if (diferenca < 0)
                {
                    // Vender excedente
                    var qtdVender = Math.Abs(diferenca);
                    var precoVenda = preco;
                    var valorVenda = qtdVender * precoVenda;
                    totalVendasMes += valorVenda;

                    var lucro = IRCalculator.CalcularLucroLiquido(
                        qtdVender, precoVenda, custodiaTicker!.PrecoMedio);
                    lucroLiquidoTotal += lucro;

                    _db.VendasRebalanceamento.Add(new VendaRebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Ticker = cestaItem.Ticker,
                        Quantidade = qtdVender,
                        PrecoVenda = precoVenda,
                        PrecoMedio = custodiaTicker.PrecoMedio,
                        DataVenda = DateTime.UtcNow
                    });

                    custodiaTicker.Quantidade -= qtdVender;
                    vendas.Add(new OperacaoRebalanceamentoResponse(
                        cestaItem.Ticker, qtdVender, precoVenda));
                }
            }

            // RN-057 a RN-062: IR sobre vendas — publicar evento completo no Kafka
            if (vendas.Any())
            {
                var irVenda = IRCalculator.CalcularIRVenda(totalVendasMes, lucroLiquidoTotal);
                var aliquota = totalVendasMes > 20_000m && lucroLiquidoTotal > 0 ? 0.20m : 0m;

                // Montar detalhes por ativo vendido (exigido pelo RN-062)
                var detalhesPorTicker = _db.VendasRebalanceamento
                    .Where(v => v.ClienteId == cliente.Id
                             && v.DataVenda >= DateTime.UtcNow.Date)
                    .Select(v => new DetalheVendaIR(
                        v.Ticker, v.Quantidade, v.PrecoVenda, v.PrecoMedio,
                        IRCalculator.CalcularLucroLiquido(v.Quantidade, v.PrecoVenda, v.PrecoMedio)))
                    .ToList();

                await _kafka.PublishAsync(
                    _kafkaSettings.TopicIRVenda,
                    cliente.Id.ToString(),
                    new IRVendaEvent(
                        Tipo:          "IR_VENDA",
                        ClienteId:     cliente.Id,
                        Cpf:           cliente.CPF,
                        MesReferencia: DateTime.UtcNow.ToString("yyyy-MM"),
                        TotalVendasMes: totalVendasMes,
                        LucroLiquido:  lucroLiquidoTotal,
                        Aliquota:      aliquota,
                        ValorIR:       irVenda,
                        Detalhes:      detalhesPorTicker,
                        DataCalculo:   DateTime.UtcNow));
            }

            operacoesPorCliente.Add(new RebalanceamentoClienteResponse(
                cliente.Id, cliente.Nome, vendas, compras));
        }

        await _db.SaveChangesAsync();

        // Identificar cesta anterior (última desativada)
        var cestaAnterior = await _db.CestasRecomendacao
            .Where(c => !c.Ativa)
            .OrderByDescending(c => c.DataDesativacao)
            .FirstOrDefaultAsync();

        return new RebalanceamentoResponse(
            cestaAnterior?.Id ?? 0,
            cestaAtiva.Id,
            operacoesPorCliente.Count,
            operacoesPorCliente,
            "Rebalanceamento concluido. Carteiras ajustadas conforme nova cesta.");
    }
}
