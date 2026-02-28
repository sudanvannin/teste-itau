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
    string Mensagem);

public record CestaItemResponse(
    string Ticker,
    decimal Percentual);

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

// ── IR ───────────────────────────────────────────────────────

public record IRNotificacaoResponse(
    string Tipo,
    int ClienteId,
    string Ticker,
    decimal ValorOperacao,
    decimal ValorIR,
    DateTime DataEvento);
