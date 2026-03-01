using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CompraProgramada.Infrastructure.Services;

public class MotorCompraService : IMotorCompraService
{
    private readonly AppDbContext _db;
    private readonly ICotacaoService _cotacaoService;
    private readonly IKafkaProducerService _kafka;
    private readonly KafkaSettings _kafkaSettings;

    public MotorCompraService(
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
    /// Executa compra consolidada na conta Master (RN-020 a RN-040).
    /// 1. Soma aportes de todos os clientes ativos (valor/3 por parcela)
    /// 2. Distribui proporcionalmente pela cesta ativa
    /// 3. Busca cotações e calcula quantidades
    /// 4. Separa lote padrão e fracionário
    /// </summary>
    public async Task<CompraConsolidadaResponse> ExecutarCompraAsync(DisparoCompraRequest request)
    {
        // RN-021: Obter cesta ativa
        var cesta = await _db.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa)
            ?? throw new BusinessException("Nenhuma cesta ativa. Configure a cesta antes de executar compras.", "CESTA_NAO_ENCONTRADA");

        // RN-022: Consolidar aportes — cada data é 1/3 do valor mensal
        var clientesAtivos = await _db.Clientes
            .Where(c => c.Ativo)
            .ToListAsync();

        if (!clientesAtivos.Any())
            throw new BusinessException("Nenhum cliente ativo para executar compra.", "SEM_CLIENTES_ATIVOS");

        var totalConsolidado = clientesAtivos.Sum(c => Math.Round(c.ValorMensal / 3m, 2));

        // RN-025: Buscar cotações dos ativos da cesta
        var tickers = cesta.Itens.Select(i => i.Ticker);
        var cotacoes = _cotacaoService.ObterCotacoesFechamento(tickers);

        if (cotacoes.Count == 0)
            throw new BusinessException("Cotacoes nao disponiveis para os ativos da cesta.", "COTACOES_INDISPONIVEIS");

        // RN-026-030: Calcular quantidade e criar ordem
        var ordem = new OrdemCompra
        {
            DataExecucao = request.DataExecucao,
            TotalConsolidado = totalConsolidado,
            Status = StatusOrdem.Executada,
            Itens = new List<OrdemCompraItem>()
        };

        var itensResponse = new List<ItemCompraResponse>();

        foreach (var cestaItem in cesta.Itens)
        {
            var valorDisponivel = totalConsolidado * (cestaItem.Percentual / 100m);
            var preco = cotacoes.GetValueOrDefault(cestaItem.Ticker, 0m);

            if (preco <= 0) continue;

            var quantidadeTotal = (int)(valorDisponivel / preco);
            if (quantidadeTotal <= 0) continue;

            // RN-031: Separar lote padrão e fracionário
            var (lotePadrao, fracionario) = LotePadraoSplitter.Separar(quantidadeTotal);

            if (lotePadrao > 0)
            {
                ordem.Itens.Add(new OrdemCompraItem
                {
                    Ticker = cestaItem.Ticker,
                    Quantidade = lotePadrao,
                    TipoMercado = TipoMercado.LotePadrao,
                    PrecoUnitario = preco
                });
                itensResponse.Add(new ItemCompraResponse(
                    cestaItem.Ticker, lotePadrao, "LOTE_PADRAO", preco,
                    Math.Round(lotePadrao * preco, 2)));
            }

            if (fracionario > 0)
            {
                var tickerFrac = LotePadraoSplitter.TickerFracionario(cestaItem.Ticker);
                ordem.Itens.Add(new OrdemCompraItem
                {
                    Ticker = tickerFrac,
                    Quantidade = fracionario,
                    TipoMercado = TipoMercado.Fracionario,
                    PrecoUnitario = preco
                });
                itensResponse.Add(new ItemCompraResponse(
                    tickerFrac, fracionario, "FRACIONARIO", preco,
                    Math.Round(fracionario * preco, 2)));
            }

            // RN-041: Atualizar custódia na conta Master
            var contaMaster = await _db.ContasGraficas
                .Include(c => c.Custodia)
                .FirstAsync(c => c.Tipo == TipoConta.Master);

            var custodiaItem = contaMaster.Custodia
                .FirstOrDefault(ci => ci.Ticker == cestaItem.Ticker);

            if (custodiaItem != null)
            {
                custodiaItem.PrecoMedio = PrecoMedioCalculator.Calcular(
                    custodiaItem.Quantidade, custodiaItem.PrecoMedio,
                    quantidadeTotal, preco);
                custodiaItem.Quantidade += quantidadeTotal;
            }
            else
            {
                contaMaster.Custodia.Add(new CustodiaItem
                {
                    Ticker = cestaItem.Ticker,
                    Quantidade = quantidadeTotal,
                    PrecoMedio = preco
                });
            }

            // Nota: IR Dedo-Duro é publicado por cliente na DistribuicaoService (RN-053 a RN-056),
            // pois somente nesse momento conhecemos o CPF e a proporção exata de cada investidor.
        }

        _db.OrdensCompra.Add(ordem);
        await _db.SaveChangesAsync();

        return new CompraConsolidadaResponse(
            ordem.Id,
            ordem.DataExecucao,
            ordem.Status.ToString(),
            ordem.TotalConsolidado,
            clientesAtivos.Count,
            itensResponse,
            "Compra consolidada executada com sucesso na conta Master.");
    }
}
