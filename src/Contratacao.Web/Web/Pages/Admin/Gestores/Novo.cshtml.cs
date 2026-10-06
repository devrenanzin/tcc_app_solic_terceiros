using System.ComponentModel.DataAnnotations;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.Gestores;

/// <summary>UC12 — Cadastrar Gestor.</summary>
internal sealed class NovoModel(IContratos contratos, AtorAtual atorAtual, CadastrarGestor cadastrar) : PaginaBase
{
    [BindProperty]
    public EntradaNovoUsuario Entrada { get; set; } = new();

    [BindProperty]
    public List<Guid> Contratos { get; set; } = [];

    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task OnGetAsync() => ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);

    public async Task<IActionResult> OnPostAsync()
    {
        ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ok = await TentarAsync(async () => await cadastrar.ExecutarAsync(
            await atorAtual.ObterAsync(), Entrada.Nome, Entrada.Email, Entrada.SenhaInicial, Contratos, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"Gestor {Entrada.Nome} cadastrado. Repasse a senha inicial a ele.");
        return Redirect("/Admin/Gestores");
    }
}

/// <summary>Dados comuns ao cadastro de Gestor e de Funcionário da Gerenciadora.</summary>
internal sealed class EntradaNovoUsuario
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150, ErrorMessage = "O nome tem no máximo 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha inicial.")]
    [Display(Name = "Senha inicial")]
    public string SenhaInicial { get; set; } = string.Empty;
}
