using Contratacao.Web.Domain.Comum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Contratacao.Web.Web.Pages;

/// <summary>Base das páginas: mensagens de retorno e tratamento das regras recusadas pelo domínio.</summary>
internal abstract class PaginaBase : PageModel
{
    internal const string ChaveSucesso = "Sucesso";
    internal const string ChaveErro = "Erro";

    protected CancellationToken Cancelamento => HttpContext.RequestAborted;

    protected void Sucesso(string mensagem) => TempData[ChaveSucesso] = mensagem;

    /// <summary>Executa a ação; se uma regra a recusar, mostra a mensagem no formulário e devolve falso.</summary>
    protected async Task<bool> TentarAsync(Func<Task> acao)
    {
        try
        {
            await acao();
            return true;
        }
        catch (RegraNegocioException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
            return false;
        }
    }

    /// <summary>Para ações de lista (desativar, reativar): a mensagem vai para o topo da página recarregada.</summary>
    protected async Task<IActionResult> ExecutarERecarregarAsync(Func<Task> acao, string mensagemSucesso)
    {
        try
        {
            await acao();
            Sucesso(mensagemSucesso);
        }
        catch (RegraNegocioException erro)
        {
            TempData[ChaveErro] = erro.Message;
        }

        return RedirectToPage();
    }
}
