using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Contratacao.Web.Web.Pages.Admin.Gestores;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.FuncionariosSesi;

/// <summary>UC14 pelo Admin: Funcionário da Gerenciadora na equipe de qualquer Gestor ativo e em qualquer contrato (Cliente).</summary>
internal sealed class NovoModel(IUsuarios usuarios, IContratos contratos, AtorAtual atorAtual, CadastrarFuncionarioSesi cadastrar) : PaginaBase
{
    [BindProperty]
    public EntradaNovoUsuario Entrada { get; set; } = new();

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
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ok = await TentarAsync(async () => await cadastrar.ExecutarAsync(
            await atorAtual.ObterAsync(), Entrada.Nome, Entrada.Email, Entrada.SenhaInicial, Contrato ?? Guid.Empty, GestorId, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"{Entrada.Nome} cadastrado(a). Repasse a senha inicial; ela será trocada no primeiro acesso.");
        return Redirect("/Admin/FuncionariosSesi");
    }

    private async Task CarregarAsync()
    {
        Gestores = [.. (await usuarios.ListarAsync(Perfil.Gestor, null, Cancelamento)).Where(g => g.Ativo)];
        Contratos = await contratos.ListarAtivosAsync(Cancelamento);
    }
}
