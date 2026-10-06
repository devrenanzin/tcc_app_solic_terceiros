using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC02 — Criar e enviar demanda. O rascunho fica só no navegador (formulario-demanda.js); o servidor só
/// recebe o envio, com os anexos escolhidos nesse momento.
/// </summary>
[RequestSizeLimit(LimiteEnvio)]
[RequestFormLimits(MultipartBodyLengthLimit = LimiteEnvio)]
internal sealed class NovaModel(ConsultarFormulario consulta, EnviarDemanda enviar, AtorAtual atorAtual) : PaginaBase
{
    /// <summary>Folga para o VP-2 e alguns anexos de até 10 MB cada; o limite por arquivo é conferido no domínio.</summary>
    internal const long LimiteEnvio = 100 * 1024 * 1024;

    /// <summary>Avisa a tela da demanda para apagar o rascunho do navegador depois do envio.</summary>
    internal const string ChaveLimparRascunho = "LimparRascunho";

    [BindProperty]
    public EntradaDemanda Entrada { get; set; } = new();

    [BindProperty]
    public IFormFile? DeAcordoVp2 { get; set; }

    [BindProperty]
    public List<IFormFile> OutrosAnexos { get; set; } = [];

    internal FormularioDemanda Formulario { get; private set; } = null!;

    /// <summary>Só na primeira abertura: depois de um envio recusado, a tela mostra o que acabou de ser enviado.</summary>
    internal bool RestaurarRascunho { get; private set; }

    internal static string ExtensoesAceitas => string.Join(",", RegraArquivo.Extensoes);

    public async Task OnGetAsync()
    {
        RestaurarRascunho = true;
        Formulario = await FormularioDemanda.CarregarAsync(consulta, Entrada, Cancelamento);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Formulario = await FormularioDemanda.CarregarAsync(consulta, Entrada, Cancelamento);

        // Números com letras chegam como erro de conversão; o domínio confere o restante.
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Confira os campos numéricos.");
            return Page();
        }

        Guid? id = null;
        var ok = await TentarAsync(async () =>
        {
            var demanda = await enviar.ExecutarAsync(
                await atorAtual.ObterAsync(),
                Entrada.ParaDados(),
                DeAcordoVp2 is null ? null : Arquivo(DeAcordoVp2),
                [.. OutrosAnexos.Select(Arquivo)],
                Cancelamento);
            id = demanda.Id;
            Sucesso($"Demanda {demanda.Numero} enviada. Ela está agora em Validação do Gestor.");
        });
        if (!ok)
        {
            return Page();
        }

        TempData[ChaveLimparRascunho] = "1";
        return Redirect($"/Demandas/Detalhe/{id}");
    }

    internal static ArquivoRecebido Arquivo(IFormFile arquivo) => new(arquivo.FileName, arquivo.Length, arquivo.OpenReadStream);
}
