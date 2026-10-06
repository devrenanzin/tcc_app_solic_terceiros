using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Usuarios;

/// <summary>UC01 — Autenticar Usuário: e-mail @ucl.br e senha própria (RN11). Usuário desativado não entra.</summary>
internal sealed class AutenticarUsuario(IUsuarios usuarios, IHashSenha hash, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    /// <returns>O usuário autenticado, ou nulo se e-mail, senha ou situação não permitirem o acesso.</returns>
    internal async Task<Usuario?> ExecutarAsync(string? email, string? senha, CancellationToken cancelamento)
    {
        string emailNormalizado;
        try
        {
            emailNormalizado = EmailUcl.Normalizar(email);
        }
        catch (RegraNegocioException)
        {
            return null;
        }

        var usuario = await usuarios.ObterPorEmailAsync(emailNormalizado, cancelamento);
        if (usuario is not { Ativo: true } || string.IsNullOrEmpty(senha) || !hash.Conferir(usuario, senha))
        {
            return null;
        }

        usuario.RegistrarAcesso(relogio.AgoraUtc);
        await unidade.SalvarAsync(cancelamento);
        return usuario;
    }
}

/// <summary>UC13 — Cadastrar-se como Solicitante (revisão de 05/10/2026).</summary>
internal sealed class CadastrarSolicitante(
    IUsuarios usuarios, IHashSenha hash, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task<Usuario> ExecutarAsync(string nome, string email, string senha, string? enderecoIp, CancellationToken cancelamento)
    {
        Senha.Validar(senha);
        var usuario = Usuario.CadastrarSolicitante(nome, email, relogio.AgoraUtc);
        await GarantirEmailLivreAsync(usuarios, usuario.Email, cancelamento);

        usuario.DefinirSenhaHash(hash.Gerar(usuario, senha));
        usuarios.Adicionar(usuario);

        var proprio = new Ator(usuario.Id, Perfil.Solicitante, true, new HashSet<Guid>(), enderecoIp);
        auditoria.Registrar(LogAuditoria.De(proprio, nameof(Usuario), usuario.Id, "AutocadastroSolicitante",
            null, usuario.Email, null, relogio.AgoraUtc));

        await unidade.SalvarAsync(cancelamento);
        return usuario;
    }

    internal static async Task GarantirEmailLivreAsync(IUsuarios usuarios, string emailNormalizado, CancellationToken cancelamento)
    {
        if (await usuarios.EmailEmUsoAsync(emailNormalizado, cancelamento))
        {
            throw new RegraNegocioException("Já existe um usuário com este e-mail.");
        }
    }
}

/// <summary>Troca de senha pelo próprio usuário; obrigatória no primeiro acesso de Gestores e Funcionários da Gerenciadora.</summary>
internal sealed class TrocarSenha(IUsuarios usuarios, IHashSenha hash, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task<Usuario> ExecutarAsync(Ator ator, string senhaAtual, string novaSenha, CancellationToken cancelamento)
    {
        var usuario = await usuarios.ObterAsync(ator.Id, cancelamento)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        if (string.IsNullOrEmpty(senhaAtual) || !hash.Conferir(usuario, senhaAtual))
        {
            throw new RegraNegocioException("A senha atual não confere.");
        }

        Senha.Validar(novaSenha);
        if (novaSenha == senhaAtual)
        {
            throw new RegraNegocioException("A nova senha precisa ser diferente da atual.");
        }

        usuario.TrocarSenha(hash.Gerar(usuario, novaSenha));
        auditoria.Registrar(LogAuditoria.De(ator, nameof(Usuario), usuario.Id, "TrocaSenha", null, null, null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
        return usuario;
    }
}

/// <summary>Monta o ator da requisição: perfil, contratos e IP. Nulo se o usuário não existe ou foi desativado.</summary>
internal sealed class ObterAtor(IUsuarios usuarios)
{
    internal async Task<Ator?> ExecutarAsync(Guid usuarioId, string? enderecoIp, CancellationToken cancelamento)
    {
        var usuario = await usuarios.ObterAsync(usuarioId, cancelamento);
        if (usuario is not { Ativo: true })
        {
            return null;
        }

        IReadOnlySet<Guid> contratos = usuario.Perfil switch
        {
            Perfil.Gestor => (await usuarios.ContratosDoGestorAsync(usuario.Id, cancelamento)).ToHashSet(),
            Perfil.FuncionarioSesi when usuario.ContratoId is { } contrato => new HashSet<Guid> { contrato },
            _ => new HashSet<Guid>(),
        };

        return new Ator(usuario.Id, usuario.Perfil, usuario.Ativo, contratos, enderecoIp);
    }
}
