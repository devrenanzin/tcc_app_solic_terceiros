using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC06 — Corrigir a demanda devolvida, pelo Solicitante que a criou; cada alteração vai para o histórico. É o
/// único momento depois do envio em que ele anexa documentos (Cliente, revisão de 06/10/2026).
/// </summary>
[RequestSizeLimit(NovaModel.LimiteEnvio)]
[RequestFormLimits(MultipartBodyLengthLimit = NovaModel.LimiteEnvio)]
internal sealed class CorrigirModel(
    ConsultarDemandas consultar, ConsultarFormulario consulta, CorrigirDemanda corrigir, AtorAtual atorAtual) : PaginaBase
{
    [BindProperty]
    public EntradaDemanda Entrada { get; set; } = new();

    [BindProperty]
    public IFormFile? NovoDeAcordoVp2 { get; set; }

    [BindProperty]
    public List<IFormFile> OutrosAnexos { get; set; } = [];

    internal DetalheDemanda Detalhe { get; private set; } = null!;
    internal FormularioDemanda Formulario { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (await CarregarAsync(id) is { } desvio)
        {
            return desvio;
        }

        Entrada = EntradaDemanda.De(Detalhe.Demanda.Dados);
        Formulario = await FormularioDemanda.CarregarAsync(consulta, Entrada, Cancelamento);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (await CarregarAsync(id) is { } desvio)
        {
            return desvio;
        }

        Formulario = await FormularioDemanda.CarregarAsync(consulta, Entrada, Cancelamento);
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Confira os campos numéricos.");
            return Page();
        }

        var anexos = OutrosAnexos.Select(a => (NovaModel.Arquivo(a), CategoriaAnexo.Geral)).ToList();
        if (NovoDeAcordoVp2 is not null)
        {
            anexos.Insert(0, (NovaModel.Arquivo(NovoDeAcordoVp2), CategoriaAnexo.DeAcordoVP2));
        }

        var ok = await TentarAsync(async () => await corrigir.ExecutarAsync(
            await atorAtual.ObterAsync(), id, Entrada.ParaDados(), anexos, Cancelamento));
        if (!ok)
        {
            return Page();
        }

        Sucesso("Correção enviada.");
        return Redirect($"/Demandas/Detalhe/{id}");
    }

    /// <summary>Carrega a demanda; devolve um desvio se ela não existir para o usuário ou não aguardar a correção dele.</summary>
    private async Task<IActionResult?> CarregarAsync(Guid id)
    {
        var detalhe = await consultar.DetalharAsync(await atorAtual.ObterAsync(), id, Cancelamento);
        if (detalhe is null)
        {
            return NotFound();
        }

        if (!detalhe.Acoes.Contains(AcaoDemanda.Corrigir))
        {
            TempData[ChaveErro] = "Esta demanda não está aguardando correção sua.";
            return Redirect($"/Demandas/Detalhe/{id}");
        }

        Detalhe = detalhe;
        return null;
    }
}
