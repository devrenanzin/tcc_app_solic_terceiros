using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Equipe;

/// <summary>Troca o contrato do grupo de um Funcionário SESI da equipe.</summary>
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
        return Redirect("/Equipe");
    }

    /// <summary>Só funcionários da equipe do Gestor logado; os de outro Gestor aparecem como inexistentes.</summary>
    private async Task<Usuario?> CarregarAsync()
    {
        var gestor = await atorAtual.ObterAsync();
        var funcionario = await usuarios.ObterAsync(Id, Cancelamento);
        if (funcionario is null || !funcionario.PodeSerGerenciadoPor(gestor) || funcionario.Perfil != Perfil.FuncionarioSesi)
        {
            return null;
        }

        NomeFuncionario = funcionario.Nome;
        ContratosDisponiveis = await contratos.ListarAtivosAsync(Cancelamento);
        return funcionario;
    }
}
