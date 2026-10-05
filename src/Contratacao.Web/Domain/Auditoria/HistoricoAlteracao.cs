using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Auditoria;

/// <summary>
/// Alteração de um campo da demanda: valor anterior e novo, com usuário, perfil e IP (seção 12–15).
/// Usado a partir da Etapa 4.
/// </summary>
internal sealed class HistoricoAlteracao
{
    private HistoricoAlteracao() { } // EF Core

    internal HistoricoAlteracao(
        Guid demandaId,
        Ator ator,
        string campo,
        string? valorAnterior,
        string? novoValor,
        string? justificativa,
        DateTime dataHoraUtc)
    {
        DemandaId = demandaId;
        UsuarioId = ator.Id;
        PerfilUsuario = ator.Perfil;
        EnderecoIp = ator.EnderecoIp;
        Campo = campo;
        ValorAnterior = valorAnterior;
        NovoValor = novoValor;
        Justificativa = justificativa;
        DataHora = dataHoraUtc;
    }

    internal Guid Id { get; private set; }
    internal Guid DemandaId { get; private set; }
    internal Guid UsuarioId { get; private set; }
    internal Perfil PerfilUsuario { get; private set; }
    internal string? EnderecoIp { get; private set; }
    internal string Campo { get; private set; } = string.Empty;
    internal string? ValorAnterior { get; private set; }
    internal string? NovoValor { get; private set; }
    internal string? Justificativa { get; private set; }
    internal DateTime DataHora { get; private set; }
}
