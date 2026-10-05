using System.Security.Claims;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Web.Pages;

/// <summary>Início: atalhos conforme o perfil. Os dashboards por perfil entram na Etapa 7.</summary>
internal sealed class IndexModel : PaginaBase
{
    internal string Nome { get; private set; } = string.Empty;
    internal Perfil? Perfil { get; private set; }

    public void OnGet()
    {
        Nome = User.Identity?.Name ?? string.Empty;
        Perfil = Enum.TryParse<Perfil>(User.FindFirstValue(ClaimTypes.Role), out var perfil) ? perfil : null;
    }
}
