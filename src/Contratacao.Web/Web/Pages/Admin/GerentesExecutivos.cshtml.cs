using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin;

/// <summary>
/// Catálogo de gerentes executivos do formulário (seção 23), mantido pelo Admin, com os corredores que cada um
/// atende (Cliente, revisão de 06/10/2026).
/// </summary>
internal sealed class GerentesExecutivosModel(ManterGerentesExecutivos manter, ConsultarFormulario consulta, AtorAtual atorAtual) : PaginaBase
{
    [BindProperty]
    public string? Nome { get; set; }

    [BindProperty]
    public List<Guid> Corredores { get; set; } = [];

    internal IReadOnlyList<GerenteExecutivo> Gerentes { get; private set; } = [];
    internal IReadOnlyList<OpcaoCorredor> CorredoresDisponiveis { get; private set; } = [];

    /// <summary>Prefixo dos ids das caixas de marcar do formulário de cadastro.</summary>
    internal string PrefixoNovo => "novo";

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var ok = await TentarAsync(async () => await manter.CadastrarAsync(
            await atorAtual.ObterAsync(), Nome ?? string.Empty, Corredores, Cancelamento));
        if (!ok)
        {
            await CarregarAsync();
            return Page();
        }

        Sucesso($"Gerente executivo {Nome?.Trim()} cadastrado.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCorredoresAsync(Guid id)
        => await ExecutarERecarregarAsync(
            async () => await manter.DefinirCorredoresAsync(await atorAtual.ObterAsync(), id, Corredores, Cancelamento),
            "Corredores do gerente atualizados.");

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await manter.AlterarSituacaoAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Gerente reativado." : "Gerente desativado: ele sai da lista do formulário e continua nas demandas já enviadas.");

    internal string NomeCorredor(Guid id)
        => CorredoresDisponiveis.FirstOrDefault(c => c.Id == id) is { } c ? $"{c.Nome} ({c.Regiao.Replace("QQP ", string.Empty, StringComparison.Ordinal)})" : "—";

    private async Task CarregarAsync()
    {
        Gerentes = await manter.ListarAsync(Cancelamento);
        CorredoresDisponiveis = (await consulta.CatalogosAsync(Cancelamento)).Corredores;
    }
}
