using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Contratacao.Web.Web.Pages;

internal sealed class IndexModel : PageModel
{
    internal string Mensagem { get; private set; } = string.Empty;

    public void OnGet() => Mensagem = "Estrutura inicial em funcionamento.";
}
