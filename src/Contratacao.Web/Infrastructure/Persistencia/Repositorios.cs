using Contratacao.Web.Application;
using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Persistencia;

internal sealed class RepositorioUsuarios(ContratacaoDbContext contexto) : IUsuarios
{
    public Task<Usuario?> ObterAsync(Guid id, CancellationToken cancelamento)
        => contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == id, cancelamento);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancelamento)
        => contexto.Usuarios.SingleOrDefaultAsync(u => u.Email == emailNormalizado, cancelamento);

    public Task<bool> EmailEmUsoAsync(string emailNormalizado, CancellationToken cancelamento)
        => contexto.Usuarios.AnyAsync(u => u.Email == emailNormalizado, cancelamento);

    // Ao ser adicionado, o usuário recebe o Id (Guid sequencial) usado em seguida na auditoria e nos vínculos.
    public void Adicionar(Usuario usuario) => contexto.Usuarios.Add(usuario);

    public async Task<IReadOnlyList<Guid>> ContratosDoGestorAsync(Guid gestorId, CancellationToken cancelamento)
        => await contexto.GestoresContratos.Where(g => g.GestorId == gestorId).Select(g => g.ContratoId).ToListAsync(cancelamento);

    public async Task DefinirContratosDoGestorAsync(Guid gestorId, IReadOnlyCollection<Guid> contratos, CancellationToken cancelamento)
    {
        var atuais = await contexto.GestoresContratos.Where(g => g.GestorId == gestorId).ToListAsync(cancelamento);
        contexto.GestoresContratos.RemoveRange(atuais.Where(g => !contratos.Contains(g.ContratoId)));
        contexto.GestoresContratos.AddRange(contratos
            .Distinct()
            .Where(c => atuais.All(g => g.ContratoId != c))
            .Select(c => new GestorContrato(gestorId, c)));
    }

    public async Task<IReadOnlyList<Guid>> AtivosDoContratoAsync(Perfil perfil, Guid contratoId, CancellationToken cancelamento)
        => perfil == Perfil.Gestor
            ? await (from gc in contexto.GestoresContratos
                     join u in contexto.Usuarios on gc.GestorId equals u.Id
                     where gc.ContratoId == contratoId && u.Ativo && u.Perfil == Perfil.Gestor
                     select u.Id).ToListAsync(cancelamento)
            : await contexto.Usuarios.Where(u => u.Perfil == perfil && u.Ativo && u.ContratoId == contratoId)
                .Select(u => u.Id).ToListAsync(cancelamento);

    public async Task<IReadOnlyList<ResumoUsuario>> ListarAsync(Perfil perfil, Guid? gestorResponsavelId, CancellationToken cancelamento)
    {
        var consulta = contexto.Usuarios.Where(u => u.Perfil == perfil);
        if (gestorResponsavelId is { } gestor)
        {
            consulta = consulta.Where(u => u.GestorResponsavelId == gestor);
        }

        var linhas = await (
            from u in consulta
            join g in contexto.Usuarios on u.GestorResponsavelId equals g.Id into gestores
            from g in gestores.DefaultIfEmpty()
            join c in contexto.Contratos on u.ContratoId equals c.Id into contratosSesi
            from c in contratosSesi.DefaultIfEmpty()
            orderby u.Nome
            select new
            {
                u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.DataCadastro, u.DataUltimoAcesso,
                u.GestorResponsavelId,
                GestorNome = g != null ? g.Nome : null,
                ContratoSesi = c != null ? c.Numero : null,
            }).ToListAsync(cancelamento);

        var ids = linhas.Select(l => l.Id).ToList();
        var contratosGestores = await (
            from gc in contexto.GestoresContratos
            join c in contexto.Contratos on gc.ContratoId equals c.Id
            where ids.Contains(gc.GestorId)
            select new { gc.GestorId, c.Numero }).ToListAsync(cancelamento);

        return [.. linhas.Select(l => new ResumoUsuario(
            l.Id, l.Nome, l.Email, l.Perfil, l.Ativo, l.DataCadastro, l.DataUltimoAcesso,
            l.ContratoSesi is { } numero
                ? [numero]
                : [.. contratosGestores.Where(c => c.GestorId == l.Id).Select(c => c.Numero).Order()],
            l.GestorResponsavelId,
            l.GestorNome))];
    }
}

internal sealed class RepositorioContratos(ContratacaoDbContext contexto) : IContratos
{
    public async Task<IReadOnlyList<ResumoContrato>> ListarAtivosAsync(CancellationToken cancelamento)
        => await contexto.Contratos.Where(c => c.Ativo).OrderBy(c => c.Numero)
            .Select(c => new ResumoContrato(c.Id, c.Numero)).ToListAsync(cancelamento);
}

internal sealed class RegistroAuditoria(ContratacaoDbContext contexto) : IAuditoria
{
    public void Registrar(LogAuditoria registro) => contexto.LogsAuditoria.Add(registro);
}

internal sealed class UnidadeDeTrabalho(ContratacaoDbContext contexto) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancelamento)
    {
        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateConcurrencyException)
        {
            // RNF10: a demanda mudou desde que foi lida; nada foi gravado.
            throw new RegraNegocioException(
                "Esta demanda foi alterada por outra pessoa enquanto você a via. Recarregue a página e tente de novo.");
        }
    }

    public async Task<T> EmTransacaoAsync<T>(Func<Task<T>> acao, CancellationToken cancelamento)
    {
        if (contexto.Database.CurrentTransaction is not null)
        {
            return await acao();
        }

        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);
        var resultado = await acao();
        await transacao.CommitAsync(cancelamento);
        return resultado;
    }
}

/// <summary>Hash de senha pelo mecanismo padrão do ASP.NET Core (seção 23).</summary>
internal sealed class HashSenha(IPasswordHasher<Usuario> hasher) : IHashSenha
{
    public string Gerar(Usuario usuario, string senha) => hasher.HashPassword(usuario, senha);

    public bool Conferir(Usuario usuario, string senha)
        => hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) is not PasswordVerificationResult.Failed;
}
