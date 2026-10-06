using Contratacao.Web.Application;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin;

/// <summary>OS de cada contrato (Cliente, revisão de 06/10/2026), mantidas pelo Admin.</summary>
internal sealed class OrdensServicoModel(ManterOrdensServico manter, IContratos contratos, AtorAtual atorAtual) : PaginaBase
{
    [BindProperty]
    public Guid? ContratoId { get; set; }

    [BindProperty]
    public string? Numero { get; set; }

    internal IReadOnlyList<OpcaoOs> Ordens { get; private set; } = [];
    internal IReadOnlyList<ResumoContrato> Contratos { get; private set; } = [];

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await TentarAsync(async () => await manter.CadastrarAsync(
            await atorAtual.ObterAsync(), ContratoId ?? Guid.Empty, Numero ?? string.Empty, Cancelamento));
        if (!ok)
        {
            await CarregarAsync();
            return Page();
        }

        Sucesso($"OS {Numero?.Trim()} cadastrada.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await manter.AlterarSituacaoAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "OS reativada." : "OS desativada: ela sai da lista do formulário e continua nas demandas já enviadas.");

    private async Task CarregarAsync()
    {
        Ordens = await manter.ListarAsync(Cancelamento);
        Contratos = await contratos.ListarAtivosAsync(Cancelamento);
    }
}
