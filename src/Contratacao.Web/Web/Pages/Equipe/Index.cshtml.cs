using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Equipe;

/// <summary>Equipe SESI do Gestor logado (UC14): só os funcionários que ele gerencia.</summary>
internal sealed class IndexModel(IUsuarios usuarios, AtorAtual atorAtual, AlterarSituacaoUsuario alterarSituacao) : PaginaBase
{
    internal IReadOnlyList<ResumoUsuario> Funcionarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var gestor = await atorAtual.ObterAsync();
        Funcionarios = await usuarios.ListarAsync(Perfil.FuncionarioSesi, gestor.Id, Cancelamento);
    }

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await alterarSituacao.ExecutarAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Funcionário reativado." : "Funcionário desativado.");
}
