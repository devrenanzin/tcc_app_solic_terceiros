using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Contratacao.Web.Web.Pages.Admin.Gestores;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Equipe;

/// <summary>UC14 — Cadastrar Funcionário SESI na equipe do Gestor logado.</summary>
internal sealed class NovoModel(IContratos contratos, AtorAtual atorAtual, CadastrarFuncionarioSesi cadastrar) : PaginaBase
{
    [BindProperty]
    public EntradaNovoUsuario Entrada { get; set; } = new();

    [BindProperty]
    public Guid? Contrato { get; set; }

    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task OnGetAsync() => await CarregarContratosAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarContratosAsync();
        if (Contrato is null)
        {
            ModelState.AddModelError(string.Empty, "Escolha o contrato do Funcionário SESI.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ok = await TentarAsync(async () => await cadastrar.ExecutarAsync(
            await atorAtual.ObterAsync(), Entrada.Nome, Entrada.Email, Entrada.SenhaInicial, Contrato!.Value, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"{Entrada.Nome} cadastrado(a) na sua equipe. Repasse a senha inicial; ela será trocada no primeiro acesso.");
        return Redirect("/Equipe");
    }

    /// <summary>Só os contratos do Gestor logado; para outro contrato, o Admin faz a troca.</summary>
    private async Task CarregarContratosAsync()
    {
        var gestor = await atorAtual.ObterAsync();
        ContratosDisponiveis = [.. (await contratos.ListarAtivosAsync(Cancelamento)).Where(c => gestor.Contratos.Contains(c.Id))];
    }
}
