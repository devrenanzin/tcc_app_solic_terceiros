using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.FuncionariosSesi;

/// <summary>Vincula uma conta já cadastrada de Solicitante como Funcionário da Gerenciadora, pelo Admin (Cliente, revisão de 06/10/2026).</summary>
internal sealed class VincularModel(IUsuarios usuarios, IContratos contratos, AtorAtual atorAtual, VincularContaComoSesi vincular) : PaginaBase
{
    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public Guid? GestorId { get; set; }

    [BindProperty]
    public Guid? Contrato { get; set; }

    internal IReadOnlyList<ResumoUsuario> Gestores { get; private set; } = [];
    internal IReadOnlyList<ResumoContrato> Contratos { get; private set; } = [];

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarAsync();
        Usuario? conta = null;
        var ok = await TentarAsync(async () => conta = await vincular.ExecutarAsync(
            await atorAtual.ObterAsync(), Email ?? string.Empty, Contrato ?? Guid.Empty, GestorId, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"A conta de {conta!.Nome} agora é Funcionário da Gerenciadora. Ela entra com o mesmo e-mail e senha.");
        return Redirect("/Admin/FuncionariosSesi");
    }

    private async Task CarregarAsync()
    {
        Gestores = [.. (await usuarios.ListarAsync(Perfil.Gestor, null, Cancelamento)).Where(g => g.Ativo)];
        Contratos = await contratos.ListarAtivosAsync(Cancelamento);
    }
}
