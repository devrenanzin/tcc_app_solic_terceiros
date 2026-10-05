namespace Contratacao.Web.Domain.Demandas;

/// <summary>Uma RAC marcada na demanda (tabela DemandaRac, seção 8.1).</summary>
internal sealed class DemandaRac
{
    private DemandaRac() { } // EF Core

    internal DemandaRac(Guid racId) => RacId = racId;

    internal Guid RacId { get; private set; }
}
