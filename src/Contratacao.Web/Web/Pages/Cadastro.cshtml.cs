using System.ComponentModel.DataAnnotations;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

/// <summary>UC13 — Cadastrar-se como Solicitante.</summary>
internal sealed class CadastroModel(CadastrarSolicitante cadastrar) : PaginaBase
{
    [BindProperty]
    public EntradaCadastro Entrada { get; set; } = new();

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? Redirect("/") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Usuario? usuario = null;
        var ok = await TentarAsync(async () => usuario = await cadastrar.ExecutarAsync(
            Entrada.Nome, Entrada.Email, Entrada.Senha, Sessao.EnderecoIp(HttpContext), Cancelamento));
        if (!ok)
        {
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Sessao.CriarPrincipal(usuario!));
        Sucesso("Cadastro concluído. Bem-vindo(a)!");
        return Redirect("/");
    }
}

internal sealed class EntradaCadastro
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150, ErrorMessage = "O nome tem no máximo 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}
