using System.ComponentModel.DataAnnotations;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

/// <summary>UC01 — Autenticar Usuário.</summary>
internal sealed class EntrarModel(AutenticarUsuario autenticar) : PaginaBase
{
    [BindProperty]
    public EntradaLogin Entrada { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Nome de quem já está conectado neste navegador; a tela oferece continuar ou trocar de conta.</summary>
    internal string? Conectado { get; private set; }

    public void OnGet() => Conectado = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await autenticar.ExecutarAsync(Entrada.Email, Entrada.Senha, Cancelamento);
        if (usuario is null)
        {
            // Mensagem única: não revela se o e-mail existe ou se o usuário está desativado.
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos, ou usuário desativado.");
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Sessao.CriarPrincipal(usuario));
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}

internal sealed class EntradaLogin
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;
}
