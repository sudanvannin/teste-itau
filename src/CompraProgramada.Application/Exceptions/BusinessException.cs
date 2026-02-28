namespace CompraProgramada.Application.Exceptions;

/// <summary>
/// Exceção de regra de negócio — resulta em HTTP 400 Bad Request.
/// Contém código de erro padronizado para a API.
/// </summary>
public class BusinessException : Exception
{
    public string Codigo { get; }

    public BusinessException(string mensagem, string codigo) : base(mensagem)
    {
        Codigo = codigo;
    }
}
