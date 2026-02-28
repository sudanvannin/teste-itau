using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Services;

public class DistribuicaoService : IDistribuicaoService
{
    private readonly AppDbContext _db;

    public DistribuicaoService(AppDbContext db) => _db = db;

    /// <summary>
    /// Distribui ativos da conta Master para as contas Filhote
    /// de forma proporcional ao aporte de cada cliente (RN-035 a RN-040).
    /// </summary>
    public async Task<DistribuicaoResponse> DistribuirAsync(int ordemCompraId)
    {
        var ordem = await _db.OrdensCompra
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == ordemCompraId)
            ?? throw new BusinessException("Ordem de compra nao encontrada.", "ORDEM_NAO_ENCONTRADA");

        var clientesAtivos = await _db.Clientes
            .Where(c => c.Ativo)
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg!.Custodia)
            .ToListAsync();

        var totalAportes = clientesAtivos.Sum(c => Math.Round(c.ValorMensal / 3m, 2));

        var distribuicoes = new List<DistribuicaoClienteResponse>();

        foreach (var cliente in clientesAtivos)
        {
            var proporcao = totalAportes > 0
                ? Math.Round((cliente.ValorMensal / 3m) / totalAportes, 6)
                : 0m;

            var distribuicao = new Distribuicao
            {
                OrdemCompraId = ordemCompraId,
                ClienteId = cliente.Id,
                DataDistribuicao = DateTime.UtcNow,
                Itens = new List<DistribuicaoItem>()
            };

            var itensCliente = new List<ItemDistribuidoResponse>();

            // Agrupar itens da ordem por ticker base (sem sufixo F)
            var itensPorTicker = ordem.Itens
                .GroupBy(i => i.Ticker.TrimEnd('F'))
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));

            foreach (var (ticker, qtdTotal) in itensPorTicker)
            {
                var qtdCliente = (int)(qtdTotal * proporcao);
                if (qtdCliente <= 0) continue;

                var precoItem = ordem.Itens
                    .FirstOrDefault(i => i.Ticker.TrimEnd('F') == ticker)?.PrecoUnitario ?? 0m;

                distribuicao.Itens.Add(new DistribuicaoItem
                {
                    Ticker = ticker,
                    Quantidade = qtdCliente,
                    PrecoUnitario = precoItem
                });

                // Atualizar custódia filhote
                var custodiaFilhote = cliente.ContaGrafica?.Custodia?
                    .FirstOrDefault(ci => ci.Ticker == ticker);

                if (custodiaFilhote != null)
                {
                    custodiaFilhote.PrecoMedio = PrecoMedioCalculator.Calcular(
                        custodiaFilhote.Quantidade, custodiaFilhote.PrecoMedio,
                        qtdCliente, precoItem);
                    custodiaFilhote.Quantidade += qtdCliente;
                }
                else
                {
                    cliente.ContaGrafica?.Custodia?.Add(new CustodiaItem
                    {
                        ContaGraficaId = cliente.ContaGrafica.Id,
                        Ticker = ticker,
                        Quantidade = qtdCliente,
                        PrecoMedio = precoItem
                    });
                }

                itensCliente.Add(new ItemDistribuidoResponse(ticker, qtdCliente, precoItem));
            }

            _db.Distribuicoes.Add(distribuicao);

            distribuicoes.Add(new DistribuicaoClienteResponse(
                cliente.Id, cliente.Nome,
                cliente.ContaGrafica?.NumeroConta ?? "",
                itensCliente));
        }

        await _db.SaveChangesAsync();

        return new DistribuicaoResponse(
            ordemCompraId,
            distribuicoes.Count,
            distribuicoes,
            "Distribuicao concluida. Ativos transferidos da Master para as contas Filhote.");
    }
}
