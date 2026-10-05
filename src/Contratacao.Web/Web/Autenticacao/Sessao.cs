using System.Security.Claims;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Contratacao.Web.Web.Autenticacao;

/// <summary>Políticas de acesso às páginas, por perfil (seção 19).</summary>
internal static class Politicas
{
    internal const string Admin = nameof(Admin);
    internal const string Gestor = nameof(Gestor);
    internal const string AdminOuGestor = nameof(AdminOuGestor);
}

/// <summary>Cookie de sessão: guarda só id, nome, e-mail e perfil. Perfil e contratos são relidos do banco a cada ação.</summary>
internal static class Sessao
{
    /// <summary>Marca a sessão de quem ainda precisa trocar a senha inicial.</summary>
    internal const string ClaimTrocarSenha = "contratacao:trocar-senha";

    internal static ClaimsPrincipal CriarPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Perfil.ToString()),
        };
        if (usuario.DeveTrocarSenha)
        {
            claims.Add(new Claim(ClaimTrocarSenha, "1"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    internal static bool DeveTrocarSenha(ClaimsPrincipal principal) => principal.HasClaim(c => c.Type == ClaimTrocarSenha);

    internal static Guid? UsuarioId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>IP de quem fez a requisição, para a auditoria (seção 12–15).</summary>
    internal static string? EnderecoIp(HttpContext contexto)
    {
        var ip = contexto.Connection.RemoteIpAddress;
        return ip is null ? null : (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
    }

    /// <summary>
    /// Derruba a sessão de quem foi desativado ou teve o perfil alterado depois de entrar
    /// (usuário desativado não acessa o sistema; seção 4.5).
    /// </summary>
    internal static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var id = contexto.Principal is { } principal ? UsuarioId(principal) : null;
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarios>();
        var usuario = id is { } valor ? await usuarios.ObterAsync(valor, contexto.HttpContext.RequestAborted) : null;

        if (usuario is not { Ativo: true } || !contexto.Principal!.IsInRole(usuario.Perfil.ToString()))
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

/// <summary>O ator da requisição atual: usuário logado, perfil, contratos e IP.</summary>
internal sealed class AtorAtual(IHttpContextAccessor acessor, ObterAtor obterAtor)
{
    private Ator? _ator;

    internal async Task<Ator> ObterAsync()
    {
        if (_ator is not null)
        {
            return _ator;
        }

        var contexto = acessor.HttpContext ?? throw new InvalidOperationException("Sem requisição HTTP.");
        var id = Sessao.UsuarioId(contexto.User) ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

        _ator = await obterAtor.ExecutarAsync(id, Sessao.EnderecoIp(contexto), contexto.RequestAborted)
            ?? throw new UnauthorizedAccessException("Usuário inexistente ou desativado.");
        return _ator;
    }
}
