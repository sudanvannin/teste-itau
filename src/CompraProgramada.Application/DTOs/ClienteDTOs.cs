namespace CompraProgramada.Application.DTOs;

// ── Adesão ───────────────────────────────────────────────────

public record AdesaoRequest(
    string Nome,
    string CPF,
    string Email,
    decimal ValorMensal);

public record AdesaoResponse(
    int ClienteId,
    string Nome,
    string CPF,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    ContaGraficaDto ContaGrafica);

public record ContaGraficaDto(
    int Id,
    string NumeroConta,
    string Tipo,
    DateTime DataCriacao);

// ── Saída ────────────────────────────────────────────────────

public record SaidaResponse(
    int ClienteId,
    string Nome,
    bool Ativo,
    DateTime? DataSaida,
    string Mensagem);

// ── Alterar Valor Mensal ─────────────────────────────────────

public record AlterarValorMensalRequest(decimal NovoValorMensal);

public record AlterarValorMensalResponse(
    int ClienteId,
    decimal ValorMensalAnterior,
    decimal ValorMensalNovo,
    DateTime DataAlteracao,
    string Mensagem);

// ── Carteira ─────────────────────────────────────────────────

public record CarteiraResponse(
    int ClienteId,
    string Nome,
    string ContaGrafica,
    DateTime DataConsulta,
    ResumoCarteiraDto Resumo,
    List<AtivoCarteiraDto> Ativos);

public record ResumoCarteiraDto(
    decimal ValorTotalInvestido,
    decimal ValorAtualCarteira,
    decimal PlTotal,
    decimal RentabilidadePercentual);

public record AtivoCarteiraDto(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal CotacaoAtual,
    decimal ValorAtual,
    decimal Pl,
    decimal PlPercentual,
    decimal ComposicaoCarteira);

// ── Rentabilidade ────────────────────────────────────────────

public record RentabilidadeResponse(
    int ClienteId,
    string Nome,
    DateTime DataConsulta,
    ResumoCarteiraDto Rentabilidade,
    List<AporteDto> HistoricoAportes,
    List<EvolucaoCarteiraDto> EvolucaoCarteira);

public record AporteDto(
    DateTime Data,
    decimal Valor,
    string Parcela);

public record EvolucaoCarteiraDto(
    DateTime Data,
    decimal ValorCarteira,
    decimal ValorInvestido,
    decimal Rentabilidade);

// ── Erro ─────────────────────────────────────────────────────

public record ErroResponse(string Erro, string Codigo);
