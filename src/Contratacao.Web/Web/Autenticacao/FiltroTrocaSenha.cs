using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Contratacao.Web.Web.Autenticacao;

/// <summary>
/// Enquanto a senha inicial não for trocada, toda página leva à troca de senha
/// (primeiro acesso de Gestores e Funcionários SESI; Cliente, revisão de 05/10/2026).
/// </summary>
internal sealed class FiltroTrocaSenha : IAsyncPageFilter
{
    private static readonly string[] PaginasLiberadas = ["/TrocarSenha", "/Sair", "/Entrar", "/AcessoNegado"];

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var pagina = context.ActionDescriptor.ViewEnginePath;
        if (Sessao.DeveTrocarSenha(context.HttpContext.User) && !PaginasLiberadas.Contains(pagina, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectResult("/TrocarSenha");
            return;
        }

        await next();
    }
}
