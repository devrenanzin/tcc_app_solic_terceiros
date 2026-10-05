using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin.Gestores;

/// <summary>Gestores cadastrados pelo Admin (UC12), com desativação e reativação.</summary>
internal sealed class IndexModel(IUsuarios usuarios, AtorAtual atorAtual, AlterarSituacaoUsuario alterarSituacao) : PaginaBase
{
    internal IReadOnlyList<ResumoUsuario> Gestores { get; private set; } = [];

    public async Task OnGetAsync() => Gestores = await usuarios.ListarAsync(Perfil.Gestor, null, Cancelamento);

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await alterarSituacao.ExecutarAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Gestor reativado." : "Gestor desativado. A equipe dele continua ativa.");
}
