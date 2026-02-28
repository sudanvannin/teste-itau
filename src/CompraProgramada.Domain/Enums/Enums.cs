namespace CompraProgramada.Domain.Enums;

public enum TipoConta
{
    Master,
    Filhote
}

public enum TipoMercado
{
    LotePadrao = 10,
    Fracionario = 20
}

public enum StatusOrdem
{
    Pendente,
    Executada,
    Cancelada
}

public enum TipoOperacao
{
    Compra,
    Venda
}
