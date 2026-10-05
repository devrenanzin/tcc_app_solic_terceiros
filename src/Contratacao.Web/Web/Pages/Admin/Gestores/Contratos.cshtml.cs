using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.Gestores;

/// <summary>Contratos de um Gestor, definidos pelo Admin.</summary>
internal sealed class ContratosModel(IUsuarios usuarios, IContratos contratos, AtorAtual atorAtual, DefinirContratosGestor definir) : PaginaBase
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public List<Guid> Contratos { get; set; } = [];

    internal string NomeGestor { get; private set; } = string.Empty;
    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CarregarAsync())
        {
            return NotFound();
        }

        Contratos = [.. await usuarios.ContratosDoGestorAsync(Id, Cancelamento)];
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await CarregarAsync())
        {
            return NotFound();
        }

        var ok = await TentarAsync(async () => await definir.ExecutarAsync(await atorAtual.ObterAsync(), Id, Contratos, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"Contratos de {NomeGestor} atualizados.");
        return Redirect("/Admin/Gestores");
    }

    private async Task<bool> CarregarAsync()
    {
        var gestor = await usuarios.ObterAsync(Id, Cancelamento);
        if (gestor is not { Perfil: Perfil.Gestor })
        {
            return false;
        }

        NomeGestor = gestor.Nome;
        ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);
        return true;
    }
}
