using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.Gestores;

/// <summary>Vincula uma conta já cadastrada de Solicitante como Gestor, com os contratos dele (Cliente, revisão de 06/10/2026).</summary>
internal sealed class VincularModel(IContratos contratos, AtorAtual atorAtual, VincularContaComoGestor vincular) : PaginaBase
{
    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public List<Guid> Contratos { get; set; } = [];

    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task OnGetAsync() => ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);

    public async Task<IActionResult> OnPostAsync()
    {
        ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);
        Usuario? conta = null;
        var ok = await TentarAsync(async () => conta = await vincular.ExecutarAsync(
            await atorAtual.ObterAsync(), Email ?? string.Empty, Contratos, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"A conta de {conta!.Nome} agora é Gestor. Ela entra com o mesmo e-mail e senha.");
        return Redirect("/Admin/Gestores");
    }
}
