using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Prazos;

/// <summary>
/// SLA da demanda (seções 9–11). Começa na aprovação do Gestor (RN03), nunca pausa nem reinicia (RN05)
/// e vence ao fim do dia limite no horário de Brasília (RN04).
/// </summary>
internal sealed class Sla
{
    private Sla() { } // EF Core

    private Sla(DateTime inicioUtc, int prazoDias, DateOnly dataLimite)
    {
        InicioUtc = inicioUtc;
        PrazoDias = prazoDias;
        DataLimite = dataLimite;
    }

    internal DateTime InicioUtc { get; private set; }

    /// <summary>Prazo vigente quando o SLA começou; mudanças posteriores do padrão não o alteram (RN07).</summary>
    internal int PrazoDias { get; private set; }

    /// <summary>Data de calendário em Brasília; a demanda vence às 23:59:59 deste dia.</summary>
    internal DateOnly DataLimite { get; private set; }

    internal DateOnly DataInicioLocal => DataLimite.AddDays(-PrazoDias);

    // SUPOSIÇÃO (S7): 45 dias corridos, contados sobre a data da aprovação em Brasília.
    internal static Sla Iniciar(DateTime aprovacaoUtc, int prazoDias, ICalendarioSla calendario)
    {
        if (prazoDias <= 0)
        {
            throw new RegraNegocioException("O prazo do SLA precisa ser maior que zero.");
        }

        var limite = calendario.DataLocal(aprovacaoUtc).AddDays(prazoDias);
        return new Sla(aprovacaoUtc, prazoDias, limite);
    }

    internal int DiasDecorridos(DateOnly hoje) => hoje.DayNumber - DataInicioLocal.DayNumber;

    internal int DiasRestantes(DateOnly hoje) => DataLimite.DayNumber - hoje.DayNumber;
}
