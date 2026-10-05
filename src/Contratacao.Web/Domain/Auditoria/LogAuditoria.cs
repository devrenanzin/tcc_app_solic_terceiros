using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Auditoria;

/// <summary>
/// Log geral de auditoria (seção 12–15, RF41): cadastros, parâmetros, transferências e operações
/// administrativas. Nunca é alterado nem excluído.
/// </summary>
internal sealed class LogAuditoria
{
    /// <summary>Perfil registrado nas ações executadas pelo próprio sistema, sem usuário.</summary>
    internal const string PerfilSistema = "Sistema";

    private LogAuditoria() { } // EF Core

    private LogAuditoria(
        Guid? usuarioId,
        string perfilUsuario,
        string? enderecoIp,
        string entidade,
        Guid? entidadeId,
        string acao,
        string? valorAnterior,
        string? novoValor,
        string? justificativa,
        DateTime dataHoraUtc)
    {
        UsuarioId = usuarioId;
        PerfilUsuario = perfilUsuario;
        EnderecoIp = enderecoIp;
        Entidade = entidade;
        EntidadeId = entidadeId;
        Acao = acao;
        ValorAnterior = valorAnterior;
        NovoValor = novoValor;
        Justificativa = justificativa;
        DataHora = dataHoraUtc;
    }

    internal Guid Id { get; private set; }
    internal Guid? UsuarioId { get; private set; }
    internal string PerfilUsuario { get; private set; } = string.Empty;
    internal string? EnderecoIp { get; private set; }
    internal string Entidade { get; private set; } = string.Empty;
    internal Guid? EntidadeId { get; private set; }
    internal string Acao { get; private set; } = string.Empty;
    internal string? ValorAnterior { get; private set; }
    internal string? NovoValor { get; private set; }
    internal string? Justificativa { get; private set; }
    internal DateTime DataHora { get; private set; }

    internal static LogAuditoria DoSistema(string entidade, Guid? entidadeId, string acao, string? novoValor, DateTime dataHoraUtc)
        => new(null, PerfilSistema, null, entidade, entidadeId, acao, null, novoValor, null, dataHoraUtc);

    internal static LogAuditoria De(
        Ator ator,
        string entidade,
        Guid? entidadeId,
        string acao,
        string? valorAnterior,
        string? novoValor,
        string? justificativa,
        DateTime dataHoraUtc)
        => new(ator.Id, ator.Perfil.ToString(), ator.EnderecoIp, entidade, entidadeId, acao, valorAnterior, novoValor, justificativa, dataHoraUtc);
}
