using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Equipe;

/// <summary>
/// O Gestor vincula uma conta já cadastrada de Solicitante como Funcionário da Gerenciadora da sua equipe, num dos seus contratos
/// (Cliente, revisão de 06/10/2026).
/// </summary>
internal sealed class VincularModel(IContratos contratos, AtorAtual atorAtual, VincularContaComoSesi vincular) : PaginaBase
{
    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public Guid? Contrato { get; set; }

    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task OnGetAsync() => await CarregarContratosAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarContratosAsync();
        Usuario? conta = null;
        var ok = await TentarAsync(async () => conta = await vincular.ExecutarAsync(
            await atorAtual.ObterAsync(), Email ?? string.Empty, Contrato ?? Guid.Empty, null, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"A conta de {conta!.Nome} agora faz parte da sua equipe da Gerenciadora, com o mesmo e-mail e senha.");
        return Redirect("/Equipe");
    }

    /// <summary>Só os contratos do Gestor logado; para outro contrato, o Admin faz o vínculo.</summary>
    private async Task CarregarContratosAsync()
    {
        var gestor = await atorAtual.ObterAsync();
        ContratosDisponiveis = [.. (await contratos.ListarAtivosAsync(Cancelamento)).Where(c => gestor.Contratos.Contains(c.Id))];
    }
}
