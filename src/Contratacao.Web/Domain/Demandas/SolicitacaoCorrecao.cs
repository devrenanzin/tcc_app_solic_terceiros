using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Uma devolução da demanda ao Solicitante (RN02c). O destino é sempre o Solicitante.</summary>
internal sealed class SolicitacaoCorrecao
{
    private SolicitacaoCorrecao() { } // EF Core

    private SolicitacaoCorrecao(Guid solicitadoPorId, OrigemCorrecao origem, TipoInconsistencia? tipo, string motivo, DateTime dataUtc)
    {
        SolicitadoPorId = solicitadoPorId;
        Origem = origem;
        Tipo = tipo;
        Motivo = motivo;
        DataSolicitacao = dataUtc;
    }

    internal Guid Id { get; private set; }
    internal Guid SolicitadoPorId { get; private set; }
    internal OrigemCorrecao Origem { get; private set; }

    /// <summary>Nulo nas devoluções do Gestor; obrigatório nas do SESI.</summary>
    internal TipoInconsistencia? Tipo { get; private set; }

    internal string Motivo { get; private set; } = string.Empty;
    internal DateTime DataSolicitacao { get; private set; }
    internal DateTime? DataResolucao { get; private set; }
    internal bool Pendente => DataResolucao is null;

    internal static SolicitacaoCorrecao PeloGestor(Guid gestorId, string motivo, DateTime dataUtc)
        => new(gestorId, OrigemCorrecao.Gestor, null, motivo, dataUtc);

    internal static SolicitacaoCorrecao PeloSesi(Guid sesiId, TipoInconsistencia tipo, string motivo, DateTime dataUtc)
        => new(sesiId, OrigemCorrecao.Sesi, tipo, motivo, dataUtc);

    internal void Resolver(DateTime dataUtc)
    {
        if (!Pendente)
        {
            throw new RegraNegocioException("Esta devolução já foi resolvida.");
        }

        DataResolucao = dataUtc;
    }
}
