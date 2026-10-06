using Contratacao.Web.Application.Auditoria;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Persistencia;

internal sealed class ConsultaAuditoria(ContratacaoDbContext contexto) : IConsultaAuditoria
{
    public async Task<PaginaAuditoria> ConsultarAsync(FiltroAuditoria filtro, int tamanhoPagina, CancellationToken cancelamento)
    {
        var consulta = contexto.LogsAuditoria.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Entidade))
        {
            consulta = consulta.Where(l => l.Entidade == filtro.Entidade);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Acao))
        {
            var acao = filtro.Acao.Trim();
            consulta = consulta.Where(l => l.Acao.Contains(acao));
        }

        if (filtro.UsuarioId is { } usuario)
        {
            consulta = consulta.Where(l => l.UsuarioId == usuario);
        }

        if (filtro.DeUtc is { } de)
        {
            consulta = consulta.Where(l => l.DataHora >= de);
        }

        if (filtro.AteUtc is { } ate)
        {
            consulta = consulta.Where(l => l.DataHora < ate);
        }

        var total = await consulta.CountAsync(cancelamento);
        var registros = await (
            from l in consulta
            join u in contexto.Usuarios on l.UsuarioId equals u.Id into usuarios
            from u in usuarios.DefaultIfEmpty()
            orderby l.DataHora descending, l.Id
            select new LinhaAuditoria(l.DataHora, l.UsuarioId, u != null ? u.Nome : null, l.PerfilUsuario, l.EnderecoIp,
                l.Entidade, l.EntidadeId, l.Acao, l.ValorAnterior, l.NovoValor, l.Justificativa))
            .Skip((filtro.Pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancelamento);

        var entidades = await contexto.LogsAuditoria.AsNoTracking().Select(l => l.Entidade).Distinct().OrderBy(e => e).ToListAsync(cancelamento);
        return new PaginaAuditoria(registros, total, filtro.Pagina, tamanhoPagina, entidades);
    }
}
