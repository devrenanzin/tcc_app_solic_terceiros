using System.ComponentModel.DataAnnotations;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

/// <summary>Troca da senha inicial no primeiro acesso (Cliente, revisão de 05/10/2026).</summary>
internal sealed class TrocarSenhaModel(AtorAtual atorAtual, TrocarSenha trocar) : PaginaBase
{
    [BindProperty]
    public EntradaTrocaSenha Entrada { get; set; } = new();

    internal bool Obrigatoria => Sessao.DeveTrocarSenha(User);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Usuario? usuario = null;
        var ok = await TentarAsync(async () => usuario = await trocar.ExecutarAsync(
            await atorAtual.ObterAsync(), Entrada.SenhaAtual, Entrada.NovaSenha, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        // Renova a sessão sem a marca de troca obrigatória.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Sessao.CriarPrincipal(usuario!));
        Sucesso("Senha alterada.");
        return Redirect("/");
    }
}

internal sealed class EntradaTrocaSenha
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}
