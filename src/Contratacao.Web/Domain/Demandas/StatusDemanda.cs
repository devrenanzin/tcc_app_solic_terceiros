namespace Contratacao.Web.Domain.Demandas;

/// <summary>Os 6 status da seção 5.2. O rascunho fica só no navegador e não é status (UC02).</summary>
internal enum StatusDemanda
{
    EmAnalise = 1,
    AguardandoCorrecao = 2,
    AguardandoResponsavel = 3,
    EmAndamento = 4,
    Concluido = 5,
    Cancelado = 6,
}
