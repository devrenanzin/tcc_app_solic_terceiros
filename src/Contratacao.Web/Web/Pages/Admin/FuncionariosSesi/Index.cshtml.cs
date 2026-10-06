using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.FuncionariosSesi;

/// <summary>
/// Todos os Funcionários SESI, com Gestor e contrato. O Admin cadastra, vincula contas existentes, troca o contrato
/// e desativa ou reativa qualquer um (Cliente, revisão de 06/10/2026).
/// </summary>
internal sealed class IndexModel(IUsuarios usuarios, AlterarSituacaoUsuario alterarSituacao, AtorAtual atorAtual) : PaginaBase
{
    internal IReadOnlyList<ResumoUsuario> Funcionarios { get; private set; } = [];

    public async Task OnGetAsync() => Funcionarios = await usuarios.ListarAsync(Perfil.FuncionarioSesi, null, Cancelamento);

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await alterarSituacao.ExecutarAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Funcionário SESI reativado." : "Funcionário SESI desativado.");
}
