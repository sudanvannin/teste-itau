namespace CompraProgramada.Infrastructure.Cotacoes;

/// <summary>
/// Modelo que representa uma cotação extraída do arquivo COTAHIST da B3.
/// Cada linha de detalhe (TIPREG = 01) gera uma instância.
/// </summary>
public class CotacaoB3
{
    /// <summary>
    /// Data do pregão (campo DATPRE, posições 3-10, formato YYYYMMDD).
    /// </summary>
    public DateTime DataPregao { get; set; }

    /// <summary>
    /// Código de negociação do ativo (campo CODNEG, posições 13-24).
    /// Ex: PETR4, VALE3, PETR4F (fracionário).
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Código BDI (campo CODBDI, posições 11-12).
    /// 02 = Lote Padrão, 96 = Fracionário.
    /// </summary>
    public string CodigoBDI { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de mercado (campo TPMERC, posições 25-27).
    /// 010 = Mercado à Vista, 020 = Fracionário.
    /// </summary>
    public int TipoMercado { get; set; }

    /// <summary>
    /// Nome resumido da empresa (campo NOMRES, posições 28-39).
    /// </summary>
    public string NomeEmpresa { get; set; } = string.Empty;

    /// <summary>
    /// Preço de abertura do pregão (campo PREABE, posições 57-69).
    /// Valor original dividido por 100 para obter reais.
    /// </summary>
    public decimal PrecoAbertura { get; set; }

    /// <summary>
    /// Preço máximo do pregão (campo PREMAX, posições 70-82).
    /// </summary>
    public decimal PrecoMaximo { get; set; }

    /// <summary>
    /// Preço mínimo do pregão (campo PREMIN, posições 83-95).
    /// </summary>
    public decimal PrecoMinimo { get; set; }

    /// <summary>
    /// Preço médio do pregão (campo PREMED, posições 96-108).
    /// </summary>
    public decimal PrecoMedio { get; set; }

    /// <summary>
    /// Preço de fechamento — último negócio do dia (campo PREULT, posições 109-121).
    /// ESTA É A COTAÇÃO UTILIZADA PARA CÁLCULO DAS COMPRAS.
    /// </summary>
    public decimal PrecoFechamento { get; set; }

    /// <summary>
    /// Quantidade total de títulos negociados (campo QUATOT, posições 153-170).
    /// </summary>
    public long QuantidadeNegociada { get; set; }

    /// <summary>
    /// Volume total negociado em reais (campo VOLTOT, posições 171-188).
    /// </summary>
    public decimal VolumeNegociado { get; set; }
}
