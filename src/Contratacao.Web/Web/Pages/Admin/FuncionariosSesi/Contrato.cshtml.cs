using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.FuncionariosSesi;

/// <summary>Admin troca o contrato de qualquer Funcionário SESI, para qualquer contrato ativo.</summary>
internal sealed class ContratoModel(IUsuarios usuarios, IContratos contratos, AtorAtual atorAtual, AlterarContratoFuncionarioSesi alterar) : PaginaBase
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid? Contrato { get; set; }

    internal string NomeFuncionario { get; private set; } = string.Empty;
    internal IReadOnlyList<ResumoContrato> ContratosDisponiveis { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var funcionario = await CarregarAsync();
        if (funcionario is null)
        {
            return NotFound();
        }

        Contrato = funcionario.ContratoId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await CarregarAsync() is null)
        {
            return NotFound();
        }

        if (Contrato is null)
        {
            ModelState.AddModelError(string.Empty, "Escolha o contrato.");
            return Page();
        }

        var ok = await TentarAsync(async () => await alterar.ExecutarAsync(await atorAtual.ObterAsync(), Id, Contrato.Value, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"Contrato de {NomeFuncionario} atualizado.");
        return Redirect("/Admin/FuncionariosSesi");
    }

    private async Task<Usuario?> CarregarAsync()
    {
        var funcionario = await usuarios.ObterAsync(Id, Cancelamento);
        if (funcionario is not { Perfil: Perfil.FuncionarioSesi })
        {
            return null;
        }

        NomeFuncionario = funcionario.Nome;
        ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);
        return funcionario;
    }
}
