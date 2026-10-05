namespace Contratacao.Web.Domain.Comum;

/// <summary>Calendário do SLA no fuso America/Sao_Paulo (RN08, RNF09).</summary>
internal sealed class CalendarioBrasilia : ICalendarioSla
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateOnly DataLocal(DateTime instanteUtc)
    {
        if (instanteUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("O instante precisa estar em UTC.", nameof(instanteUtc));
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instanteUtc, Fuso));
    }
}
