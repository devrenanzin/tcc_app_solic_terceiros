namespace Contratacao.Web.Domain.Comum;

/// <summary>Converte instantes UTC para a data de calendário usada na contagem do SLA (RN08).</summary>
internal interface ICalendarioSla
{
    DateOnly DataLocal(DateTime instanteUtc);
}
