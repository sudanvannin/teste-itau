using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Services;

public class CestaService : ICestaService
{
    private readonly AppDbContext _db;

    public CestaService(AppDbContext db) => _db = db;

    /// <summary>
    /// Cria ou substitui a cesta de recomendação ativa (RN-014 a RN-019).
    /// </summary>
    public async Task<CestaResponse> CriarOuSubstituirAsync(CriarCestaRequest request)
    {
        // RN-015: Exatamente 5 ativos
        if (request.Itens.Count != 5)
            throw new BusinessException("A cesta deve conter exatamente 5 ativos.", "CESTA_ITENS_INVALIDO");

        // RN-015: Soma dos percentuais deve ser 100%
        var somaPercentuais = request.Itens.Sum(i => i.Percentual);
        if (Math.Abs(somaPercentuais - 100m) > 0.01m)
            throw new BusinessException("A soma dos percentuais deve ser exatamente 100%.", "CESTA_PERCENTUAL_INVALIDO");

        // RN-016: Tickers únicos
        var tickersDistintos = request.Itens.Select(i => i.Ticker.ToUpper()).Distinct().Count();
        if (tickersDistintos != 5)
            throw new BusinessException("Os tickers devem ser unicos.", "CESTA_TICKERS_DUPLICADOS");

        // RN-018: Desativar cesta anterior
        var cestaAnterior = await _db.CestasRecomendacao
            .FirstOrDefaultAsync(c => c.Ativa);

        if (cestaAnterior != null)
        {
            cestaAnterior.Ativa = false;
            cestaAnterior.DataDesativacao = DateTime.UtcNow;
        }

        // Criar nova cesta
        var novaCesta = new CestaRecomendacao
        {
            Nome = request.Nome,
            Ativa = true,
            DataCriacao = DateTime.UtcNow,
            Itens = request.Itens.Select(i => new CestaItem
            {
                Ticker = i.Ticker.ToUpper(),
                Percentual = i.Percentual
            }).ToList()
        };

        _db.CestasRecomendacao.Add(novaCesta);
        await _db.SaveChangesAsync();

        var mensagem = cestaAnterior != null
            ? "Cesta atualizada. A cesta anterior foi desativada. Rebalanceamento necessario."
            : "Cesta criada com sucesso.";

        return new CestaResponse(
            novaCesta.Id,
            novaCesta.Nome,
            novaCesta.Ativa,
            novaCesta.DataCriacao,
            novaCesta.Itens.Select(i => new CestaItemResponse(i.Ticker, i.Percentual)).ToList(),
            mensagem);
    }

    /// <summary>
    /// Obtém a cesta ativa atual.
    /// </summary>
    public async Task<CestaResponse> ObterAtivaAsync()
    {
        var cesta = await _db.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa)
            ?? throw new BusinessException("Nenhuma cesta ativa encontrada.", "CESTA_NAO_ENCONTRADA");

        return new CestaResponse(
            cesta.Id, cesta.Nome, cesta.Ativa, cesta.DataCriacao,
            cesta.Itens.Select(i => new CestaItemResponse(i.Ticker, i.Percentual)).ToList(),
            "Cesta ativa encontrada.");
    }
}
