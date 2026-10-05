namespace Contratacao.Web.Domain.Auditoria;

/// <summary>Alteração de um campo da demanda: valor anterior e novo (seção 12–15). Usado a partir da Etapa 4.</summary>
internal sealed class HistoricoAlteracao
{
    private HistoricoAlteracao() { } // EF Core

    internal HistoricoAlteracao(
        Guid demandaId,
        Guid usuarioId,
        string campo,
        string? valorAnterior,
        string? novoValor,
        string? justificativa,
        DateTime dataHoraUtc)
    {
        DemandaId = demandaId;
        UsuarioId = usuarioId;
        Campo = campo;
        ValorAnterior = valorAnterior;
        NovoValor = novoValor;
        Justificativa = justificativa;
        DataHora = dataHoraUtc;
    }

    internal Guid Id { get; private set; }
    internal Guid DemandaId { get; private set; }
    internal Guid UsuarioId { get; private set; }
    internal string Campo { get; private set; } = string.Empty;
    internal string? ValorAnterior { get; private set; }
    internal string? NovoValor { get; private set; }
    internal string? Justificativa { get; private set; }
    internal DateTime DataHora { get; private set; }
}
