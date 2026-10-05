using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

internal sealed class SairModel : PaginaBase
{
    public IActionResult OnGet() => Redirect("/");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/Entrar");
    }
}
