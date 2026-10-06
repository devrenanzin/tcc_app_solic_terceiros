using Contratacao.Web.Application.Notificacoes;
using Contratacao.Web.Domain.Notificacoes;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Persistencia;

internal sealed class RepositorioNotificacoes(ContratacaoDbContext contexto) : INotificacoes
{
    public void Adicionar(Notificacao notificacao) => contexto.Notificacoes.Add(notificacao);

    public Task<bool> ExisteAsync(Guid demandaId, TipoNotificacao tipo, CancellationToken cancelamento)
        => contexto.Notificacoes.AnyAsync(n => n.DemandaId == demandaId && n.Tipo == tipo, cancelamento);

    public async Task<IReadOnlyList<Notificacao>> DoUsuarioAsync(Guid usuarioId, int limite, CancellationToken cancelamento)
        => await contexto.Notificacoes.AsNoTracking().Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.DataCriacao).ThenBy(n => n.Id).Take(limite).ToListAsync(cancelamento);

    public Task<int> NaoLidasAsync(Guid usuarioId, CancellationToken cancelamento)
        => contexto.Notificacoes.CountAsync(n => n.UsuarioId == usuarioId && n.DataLeitura == null, cancelamento);

    public Task<Notificacao?> ObterAsync(Guid id, CancellationToken cancelamento)
        => contexto.Notificacoes.SingleOrDefaultAsync(n => n.Id == id, cancelamento);

    public async Task<IReadOnlyList<Notificacao>> NaoLidasDoUsuarioAsync(Guid usuarioId, CancellationToken cancelamento)
        => await contexto.Notificacoes.Where(n => n.UsuarioId == usuarioId && n.DataLeitura == null).ToListAsync(cancelamento);
}
