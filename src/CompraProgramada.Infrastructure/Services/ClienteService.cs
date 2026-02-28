using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _db;
    private readonly ICotacaoService _cotacaoService;

    public ClienteService(AppDbContext db, ICotacaoService cotacaoService)
    {
        _db = db;
        _cotacaoService = cotacaoService;
    }

    /// <summary>
    /// Adesão ao produto (RN-001 a RN-006).
    /// Cria cliente + conta gráfica filhote.
    /// </summary>
    public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
    {
        // RN-002: CPF único
        if (await _db.Clientes.AnyAsync(c => c.CPF == request.CPF))
            throw new BusinessException("CPF ja cadastrado no sistema.", "CLIENTE_CPF_DUPLICADO");

        // RN-003: Valor mínimo
        if (request.ValorMensal < 100m)
            throw new BusinessException("O valor mensal minimo e de R$ 100,00.", "VALOR_MENSAL_INVALIDO");

        // Gerar número sequencial para conta filhote
        var maxConta = await _db.ContasGraficas
            .Where(c => c.Tipo == TipoConta.Filhote)
            .CountAsync();
        var numeroConta = $"FLH-{(maxConta + 1):D6}";

        var cliente = new Cliente
        {
            Nome = request.Nome,
            CPF = request.CPF,
            Email = request.Email,
            ValorMensal = request.ValorMensal,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };

        var contaGrafica = new ContaGrafica
        {
            NumeroConta = numeroConta,
            Tipo = TipoConta.Filhote,
            Cliente = cliente,
            DataCriacao = DateTime.UtcNow
        };

        _db.Clientes.Add(cliente);
        _db.ContasGraficas.Add(contaGrafica);
        await _db.SaveChangesAsync();

        return new AdesaoResponse(
            cliente.Id,
            cliente.Nome,
            cliente.CPF,
            cliente.Email,
            cliente.ValorMensal,
            cliente.Ativo,
            cliente.DataAdesao,
            new ContaGraficaDto(
                contaGrafica.Id,
                contaGrafica.NumeroConta,
                contaGrafica.Tipo.ToString().ToUpper(),
                contaGrafica.DataCriacao));
    }

    /// <summary>
    /// Saída do produto (RN-007 a RN-010).
    /// </summary>
    public async Task<SaidaResponse> SairAsync(int clienteId)
    {
        var cliente = await _db.Clientes.FindAsync(clienteId)
            ?? throw new BusinessException("Cliente nao encontrado.", "CLIENTE_NAO_ENCONTRADO");

        if (!cliente.Ativo)
            throw new BusinessException("Cliente ja havia saido do produto.", "CLIENTE_JA_INATIVO");

        cliente.Ativo = false;
        cliente.DataSaida = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new SaidaResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Ativo,
            cliente.DataSaida,
            "Adesao encerrada. Sua posicao em custodia foi mantida.");
    }

    /// <summary>
    /// Alteração do valor mensal (RN-011 a RN-013).
    /// </summary>
    public async Task<AlterarValorMensalResponse> AlterarValorMensalAsync(int clienteId, AlterarValorMensalRequest request)
    {
        var cliente = await _db.Clientes.FindAsync(clienteId)
            ?? throw new BusinessException("Cliente nao encontrado.", "CLIENTE_NAO_ENCONTRADO");

        if (request.NovoValorMensal < 100m)
            throw new BusinessException("O valor mensal minimo e de R$ 100,00.", "VALOR_MENSAL_INVALIDO");

        var valorAnterior = cliente.ValorMensal;

        // RN-013: Manter histórico
        _db.HistoricoValoresMensais.Add(new HistoricoValorMensal
        {
            ClienteId = clienteId,
            ValorAnterior = valorAnterior,
            ValorNovo = request.NovoValorMensal,
            DataAlteracao = DateTime.UtcNow
        });

        cliente.ValorMensal = request.NovoValorMensal;
        await _db.SaveChangesAsync();

        return new AlterarValorMensalResponse(
            cliente.Id,
            valorAnterior,
            cliente.ValorMensal,
            DateTime.UtcNow,
            "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra.");
    }

    /// <summary>
    /// Consulta de carteira com P/L (RN-063 a RN-070).
    /// </summary>
    public async Task<CarteiraResponse> ConsultarCarteiraAsync(int clienteId)
    {
        var cliente = await _db.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg!.Custodia)
            .FirstOrDefaultAsync(c => c.Id == clienteId)
            ?? throw new BusinessException("Cliente nao encontrado.", "CLIENTE_NAO_ENCONTRADO");

        var custodia = cliente.ContaGrafica?.Custodia?.ToList() ?? new List<CustodiaItem>();

        var tickers = custodia.Select(c => c.Ticker).Distinct();
        var cotacoes = _cotacaoService.ObterCotacoesFechamento(tickers);

        var ativos = custodia.Select(item =>
        {
            var cotacaoAtual = cotacoes.GetValueOrDefault(item.Ticker, item.PrecoMedio);
            var valorAtual = item.Quantidade * cotacaoAtual;
            var pl = (cotacaoAtual - item.PrecoMedio) * item.Quantidade;
            var plPercentual = item.PrecoMedio > 0
                ? Math.Round(((cotacaoAtual - item.PrecoMedio) / item.PrecoMedio) * 100, 2)
                : 0m;

            return new AtivoCarteiraDto(
                item.Ticker, item.Quantidade, item.PrecoMedio,
                cotacaoAtual, Math.Round(valorAtual, 2),
                Math.Round(pl, 2), plPercentual, 0m);
        }).ToList();

        var valorAtualCarteira = ativos.Sum(a => a.ValorAtual);

        ativos = ativos.Select(a => a with
        {
            ComposicaoCarteira = valorAtualCarteira > 0
                ? Math.Round((a.ValorAtual / valorAtualCarteira) * 100, 2) : 0m
        }).ToList();

        var valorInvestido = custodia.Sum(c => c.Quantidade * c.PrecoMedio);
        var plTotal = ativos.Sum(a => a.Pl);
        var rentabilidade = valorInvestido > 0
            ? Math.Round(((valorAtualCarteira - valorInvestido) / valorInvestido) * 100, 2) : 0m;

        return new CarteiraResponse(
            cliente.Id, cliente.Nome,
            cliente.ContaGrafica?.NumeroConta ?? "",
            DateTime.UtcNow,
            new ResumoCarteiraDto(
                Math.Round(valorInvestido, 2), Math.Round(valorAtualCarteira, 2),
                Math.Round(plTotal, 2), rentabilidade),
            ativos);
    }

    /// <summary>
    /// Consulta de rentabilidade detalhada.
    /// </summary>
    public async Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(int clienteId)
    {
        var carteira = await ConsultarCarteiraAsync(clienteId);

        var distribuicoes = await _db.Distribuicoes
            .Where(d => d.ClienteId == clienteId)
            .OrderBy(d => d.DataDistribuicao)
            .Include(d => d.Itens)
            .ToListAsync();

        var aportes = distribuicoes.Select((d, i) => new AporteDto(
            d.DataDistribuicao,
            d.Itens.Sum(item => item.Quantidade * item.PrecoUnitario),
            $"{(i % 3) + 1}/3"
        )).ToList();

        decimal acumuladoInvestido = 0m;
        var evolucao = aportes.Select(a =>
        {
            acumuladoInvestido += a.Valor;
            return new EvolucaoCarteiraDto(a.Data, 0m, acumuladoInvestido, 0m);
        }).ToList();

        if (evolucao.Count > 0)
        {
            evolucao[^1] = evolucao[^1] with
            {
                ValorCarteira = carteira.Resumo.ValorAtualCarteira,
                Rentabilidade = carteira.Resumo.RentabilidadePercentual
            };
        }

        return new RentabilidadeResponse(
            carteira.ClienteId, carteira.Nome, DateTime.UtcNow,
            carteira.Resumo, aportes, evolucao);
    }
}
