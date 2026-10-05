using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Solicitantes;

/// <summary>Solicitantes autocadastrados; o Admin e qualquer Gestor os desativam e reativam.</summary>
internal sealed class IndexModel(IUsuarios usuarios, AtorAtual atorAtual, AlterarSituacaoUsuario alterarSituacao) : PaginaBase
{
    internal IReadOnlyList<ResumoUsuario> Solicitantes { get; private set; } = [];

    public async Task OnGetAsync() => Solicitantes = await usuarios.ListarAsync(Perfil.Solicitante, null, Cancelamento);

    public async Task<IActionResult> OnPostSituacaoAsync(Guid id, bool ativo)
        => await ExecutarERecarregarAsync(
            async () => await alterarSituacao.ExecutarAsync(await atorAtual.ObterAsync(), id, ativo, Cancelamento),
            ativo ? "Solicitante reativado." : "Solicitante desativado.");
}
