using System.Globalization;
using System.Text;

namespace CompraProgramada.Infrastructure.Cotacoes;

/// <summary>
/// Faz o parse do arquivo COTAHIST da B3.
/// O formato é um arquivo texto de campos com tamanho fixo (245 caracteres por linha).
/// Processa apenas registros de detalhe (TIPREG = 01) do mercado à vista (010) e fracionário (020).
/// </summary>
public class CotahistParser
{
    // Registra o encoding provider para suportar ISO-8859-1 (Latin1)
    static CotahistParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Lê e faz parse de um arquivo COTAHIST da B3.
    /// Retorna apenas registros de detalhe (TIPREG = 01)
    /// filtrados por mercado à vista (010) e fracionário (020).
    /// </summary>
    /// <param name="caminhoArquivo">Caminho completo do arquivo .TXT</param>
    /// <returns>Lista de cotações parseadas</returns>
    public IEnumerable<CotacaoB3> ParseArquivo(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
            throw new FileNotFoundException($"Arquivo COTAHIST não encontrado: {caminhoArquivo}");

        var encoding = Encoding.GetEncoding("ISO-8859-1");
        var cotacoes = new List<CotacaoB3>();

        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            // Ignorar linhas com menos de 245 caracteres (header/trailer incompletos)
            if (linha.Length < 245)
                continue;

            // Filtrar apenas registros de detalhe (TIPREG = 01)
            var tipoRegistro = linha.Substring(0, 2);
            if (tipoRegistro != "01")
                continue;

            // Filtrar apenas mercado à vista (010) e fracionário (020)
            var tipoMercado = int.Parse(linha.Substring(24, 3).Trim());
            if (tipoMercado != 10 && tipoMercado != 20)
                continue;

            var cotacao = new CotacaoB3
            {
                DataPregao = DateTime.ParseExact(
                    linha.Substring(2, 8), "yyyyMMdd",
                    CultureInfo.InvariantCulture),
                CodigoBDI = linha.Substring(10, 2).Trim(),
                Ticker = linha.Substring(12, 12).Trim(),
                TipoMercado = tipoMercado,
                NomeEmpresa = linha.Substring(27, 12).Trim(),
                PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                PrecoMedio = ParsePreco(linha.Substring(95, 13)),
                PrecoFechamento = ParsePreco(linha.Substring(108, 13)),
                QuantidadeNegociada = long.Parse(linha.Substring(152, 18).Trim()),
                VolumeNegociado = ParsePreco(linha.Substring(170, 18))
            };

            cotacoes.Add(cotacao);
        }

        return cotacoes;
    }

    /// <summary>
    /// Converte o valor inteiro do arquivo para decimal com 2 casas decimais.
    /// Os preços no COTAHIST têm 2 casas decimais implícitas.
    /// Ex: "0000000003850" → 38.50m
    /// </summary>
    private static decimal ParsePreco(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor / 100m;
        return 0m;
    }
}
