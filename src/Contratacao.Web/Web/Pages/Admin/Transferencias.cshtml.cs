using System.ComponentModel.DataAnnotations;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin;

/// <summary>UC19 — Transferir Vínculo de Usuário: Funcionários SESI de um Gestor para outro.</summary>
internal sealed class TransferenciasModel(IUsuarios usuarios, AtorAtual atorAtual, TransferirVinculo transferir) : PaginaBase
{
    [BindProperty(SupportsGet = true)]
    public Guid? Origem { get; set; }

    [BindProperty]
    public List<Guid> Funcionarios { get; set; } = [];

    [BindProperty]
    public Guid? Destino { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Informe a justificativa.")]
    [StringLength(1000, ErrorMessage = "A justificativa tem no máximo 1000 caracteres.")]
    public string Justificativa { get; set; } = string.Empty;

    internal IReadOnlyList<ResumoUsuario> Gestores { get; private set; } = [];
    internal IReadOnlyList<ResumoUsuario> Equipe { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ModelState.Clear();
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarAsync();
        if (Destino is null)
        {
            ModelState.AddModelError(nameof(Destino), "Escolha o Gestor de destino.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ok = await TentarAsync(async () => await transferir.ExecutarAsync(
            await atorAtual.ObterAsync(), Funcionarios, Destino!.Value, Justificativa, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso($"{Funcionarios.Count} vínculo(s) transferido(s).");
        return Redirect($"/Admin/Transferencias?origem={Destino}");
    }

    private async Task CarregarAsync()
    {
        Gestores = await usuarios.ListarAsync(Perfil.Gestor, null, Cancelamento);
        Equipe = Origem is { } origem
            ? await usuarios.ListarAsync(Perfil.FuncionarioSesi, origem, Cancelamento)
            : [];
    }
}
