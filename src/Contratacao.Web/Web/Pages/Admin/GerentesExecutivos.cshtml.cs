using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin;

/// <summary>Catálogo de gerentes executivos do formulário (seção 23), mantido pelo Admin.</summary>
internal sealed class GerentesExecutivosModel(ManterGerentesExecutivos manter, AtorAtual atorAtual) : PaginaBase
{
    [BindProperty]
    public string? Nome { get; set; }

    internal IReadOnlyList<GerenteExecutivo> Gerentes { get; private set; } = [];

    public async Task OnGetAsync() => Gerentes = await manter.ListarAsync(Cancelamento);

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await TentarAsync(async () => await manter.CadastrarAsync(await atorAtual.ObterAsync(), Nome ?? string.Empty, Cancelamento));
        if (!ok)
        {
            Gerentes = await manter.ListarAsync(Cancelamento);
            return Page();
        }

        Sucesso($"Gerente executivo {Nome?.Trim()} cadastrado.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await manter.AlterarSituacaoAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Gerente reativado." : "Gerente desativado: ele sai da lista do formulário e continua nas demandas já enviadas.");
}
