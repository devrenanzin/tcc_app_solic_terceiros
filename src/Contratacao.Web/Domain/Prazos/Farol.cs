namespace Contratacao.Web.Domain.Prazos;

/// <summary>Cores do farol (seções 9–11).</summary>
internal enum Farol
{
    Cinza = 1,
    Verde = 2,
    Amarelo = 3,
    Laranja = 4,
    Vermelho = 5,

    /// <summary>Demanda cancelada: X vermelho com o rótulo Cancelado (Cliente).</summary>
    Cancelado = 6,
}
