using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

/// <summary>
/// Encerra a sessão. Abrir /Sair mostra a confirmação; sair de fato é um POST com token antifalsificação,
/// para que um link de outro site não derrube a sessão de ninguém.
/// </summary>
internal sealed class SairModel : PaginaBase
{
    internal string Nome { get; private set; } = string.Empty;
    internal string Email { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Redirect("/Entrar");
        }

        Nome = User.Identity.Name ?? string.Empty;
        Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData[ChaveSucesso] = "Você saiu do sistema. Entre com outra conta, se quiser.";
        return Redirect("/Entrar");
    }
}
