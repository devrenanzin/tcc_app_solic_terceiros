using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Auditoria;

/// <summary>Um registro do log de auditoria, com o nome de quem o fez.</summary>
internal sealed record LinhaAuditoria(
    DateTime DataHora,
    Guid? UsuarioId,
    string? Usuario,
    string PerfilUsuario,
    string? EnderecoIp,
    string Entidade,
    Guid? EntidadeId,
    string Acao,
    string? ValorAnterior,
    string? NovoValor,
    string? Justificativa);

/// <summary>Filtros da consulta: entidade, ação (parte do nome), usuário e período em UTC.</summary>
internal sealed record FiltroAuditoria(string? Entidade, string? Acao, Guid? UsuarioId, DateTime? DeUtc, DateTime? AteUtc, int Pagina);

internal sealed record PaginaAuditoria(IReadOnlyList<LinhaAuditoria> Registros, int Total, int Pagina, int TamanhoPagina, IReadOnlyList<string> Entidades);

internal interface IConsultaAuditoria
{
    Task<PaginaAuditoria> ConsultarAsync(FiltroAuditoria filtro, int tamanhoPagina, CancellationToken cancelamento);
}

/// <summary>Consultar a auditoria completa: só o Admin (seção 19). O log nunca é alterado nem excluído.</summary>
internal sealed class ConsultarAuditoria(IConsultaAuditoria consulta)
{
    internal const int TamanhoPagina = 50;

    internal Task<PaginaAuditoria> ExecutarAsync(Ator ator, FiltroAuditoria filtro, CancellationToken cancelamento)
    {
        if (!ator.Eh(Perfil.Admin))
        {
            throw new RegraNegocioException("Só o Admin consulta a auditoria completa.");
        }

        return consulta.ConsultarAsync(filtro with { Pagina = Math.Max(1, filtro.Pagina) }, TamanhoPagina, cancelamento);
    }
}
