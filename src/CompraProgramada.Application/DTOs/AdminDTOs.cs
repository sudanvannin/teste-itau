namespace CompraProgramada.Application.DTOs;

// ── Cesta de Recomendação ────────────────────────────────────

public record CriarCestaRequest(
    string Nome,
    List<CestaItemRequest> Itens);

public record CestaItemRequest(
    string Ticker,
    decimal Percentual);

public record CestaResponse(
    int CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    List<CestaItemResponse> Itens,
    string Mensagem)
{
    public DateTime? DataDesativacao { get; init; }
}

public record CestaItemResponse(
    string Ticker,
    decimal Percentual);

/// <summary>
/// Resposta do endpoint GET /api/admin/cesta/historico.
/// Todas as cestas (ativas e inativas), ordenadas da mais recente para a mais antiga.
/// </summary>
public record HistoricoCestasResponse(
    int TotalCestas,
    CestaResponse? CestaAtiva,
    List<CestaResponse> Historico);

// ── Motor de Compra ──────────────────────────────────────────

public record DisparoCompraRequest(DateTime DataExecucao);

public record CompraConsolidadaResponse(
    int OrdemCompraId,
    DateTime DataExecucao,
    string Status,
    decimal TotalConsolidado,
    int TotalClientes,
    List<ItemCompraResponse> Itens,
    string Mensagem);

public record ItemCompraResponse(
    string Ticker,
    int Quantidade,
    string TipoMercado,
    decimal PrecoUnitario,
    decimal TotalItem);

// ── Distribuição ─────────────────────────────────────────────

public record DistribuicaoResponse(
    int OrdemCompraId,
    int TotalClientes,
    List<DistribuicaoClienteResponse> Distribuicoes,
    string Mensagem);

public record DistribuicaoClienteResponse(
    int ClienteId,
    string Nome,
    string ContaGrafica,
    List<ItemDistribuidoResponse> Itens);

public record ItemDistribuidoResponse(
    string Ticker,
    int Quantidade,
    decimal PrecoUnitario);

// ── Rebalanceamento ──────────────────────────────────────────

public record RebalanceamentoResponse(
    int CestaAnteriorId,
    int CestaNovaId,
    int TotalClientesAfetados,
    List<RebalanceamentoClienteResponse> Operacoes,
    string Mensagem);

public record RebalanceamentoClienteResponse(
    int ClienteId,
    string Nome,
    List<OperacaoRebalanceamentoResponse> Vendas,
    List<OperacaoRebalanceamentoResponse> Compras);

public record OperacaoRebalanceamentoResponse(
    string Ticker,
    int Quantidade,
    decimal Preco);

// ── IR — Eventos Kafka (RN-056 / RN-062) ─────────────────────

/// <summary>
/// Evento publicado no tópico ir-dedo-duro a cada distribuição ao cliente (RN-056).
/// Contém todos os campos exigidos para rastreamento da Receita Federal.
/// </summary>
public record IRDedoDuroEvent(
    string Tipo,            // "IR_DEDO_DURO"
    int ClienteId,
    string Cpf,
    string Ticker,
    string TipoOperacao,    // "COMPRA"
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorOperacao,
    decimal Aliquota,       // 0.00005
    decimal ValorIR,
    DateTime DataOperacao);

/// <summary>
/// Evento publicado no tópico ir-venda após rebalanceamento com vendas > R$20k (RN-062).
/// </summary>
public record IRVendaEvent(
    string Tipo,            // "IR_VENDA"
    int ClienteId,
    string Cpf,
    string MesReferencia,   // "2026-03"
    decimal TotalVendasMes,
    decimal LucroLiquido,
    decimal Aliquota,       // 0.20
    decimal ValorIR,
    List<DetalheVendaIR> Detalhes,
    DateTime DataCalculo);

public record DetalheVendaIR(
    string Ticker,
    int Quantidade,
    decimal PrecoVenda,
    decimal PrecoMedio,
    decimal Lucro);
