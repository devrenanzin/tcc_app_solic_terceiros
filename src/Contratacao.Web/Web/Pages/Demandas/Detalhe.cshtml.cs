using System.Security.Claims;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC03 e UC15 — Consultar a demanda; daqui o Gestor do contrato aprova ou devolve (UC04, UC05) e o SESI do
/// contrato aceita ou devolve (UC05, UC07). Só aparecem as ações que o domínio permite ao usuário.
/// </summary>
internal sealed class DetalheModel(
    ConsultarDemandas consultar,
    AprovarDemanda aprovar,
    DevolverDemandaPeloGestor devolverPeloGestor,
    DevolverDemandaPeloSesi devolverPeloSesi,
    AceitarDemanda aceitar,
    AtorAtual atorAtual,
    IRelogio relogio,
    ICalendarioSla calendario) : PaginaBase
{
    internal DetalheDemanda Detalhe { get; private set; } = null!;
    internal DateOnly Hoje { get; private set; }

    /// <summary>Chave do rascunho deste usuário, para apagá-lo logo depois do envio.</summary>
    internal string? RascunhoParaLimpar { get; private set; }

    [BindProperty]
    public string? Motivo { get; set; }

    [BindProperty]
    public TipoInconsistencia? Tipo { get; set; }

    internal Demanda Demanda => Detalhe.Demanda;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!await CarregarAsync(id))
        {
            return NotFound();
        }

        if (TempData[NovaModel.ChaveLimparRascunho] is not null)
        {
            RascunhoParaLimpar = $"contratacao.rascunho.{User.FindFirstValue(ClaimTypes.NameIdentifier)}";
        }

        return Page();
    }

    public Task<IActionResult> OnPostAprovarAsync(Guid id)
        => ExecutarAsync(async ator => await aprovar.ExecutarAsync(ator, id, Cancelamento),
            "Demanda aprovada. Ela segue para a Validação SESI e o prazo de SLA começou.");

    public Task<IActionResult> OnPostDevolverGestorAsync(Guid id)
        => ExecutarAsync(async ator => await devolverPeloGestor.ExecutarAsync(ator, id, Motivo ?? string.Empty, Cancelamento),
            "Demanda devolvida ao Solicitante para correção.");

    public Task<IActionResult> OnPostAceitarAsync(Guid id)
        => ExecutarAsync(async ator => await aceitar.ExecutarAsync(ator, id, Cancelamento),
            "Demanda aceita. Você é o responsável SESI e ela está em Recrutamento.");

    public Task<IActionResult> OnPostDevolverSesiAsync(Guid id)
        => ExecutarAsync(async ator =>
        {
            if (Tipo is not { } tipo)
            {
                throw new RegraNegocioException("Informe o tipo da inconsistência.");
            }

            await devolverPeloSesi.ExecutarAsync(ator, id, tipo, Motivo ?? string.Empty, Cancelamento);
        }, "Demanda devolvida ao Solicitante para correção.");

    /// <summary>Download de um anexo, para quem pode ver a demanda.</summary>
    public async Task<IActionResult> OnGetAnexoAsync(Guid anexoId)
    {
        var arquivo = await consultar.AnexoAsync(await atorAtual.ObterAsync(), anexoId, Cancelamento);
        if (arquivo is not { } encontrado)
        {
            return NotFound();
        }

        return File(encontrado.Abrir(), encontrado.Anexo.TipoArquivo, encontrado.Anexo.NomeArquivo);
    }

    private async Task<IActionResult> ExecutarAsync(Func<Domain.Usuarios.Ator, Task> acao, string mensagem)
    {
        var ator = await atorAtual.ObterAsync();
        return await ExecutarERecarregarAsync(() => acao(ator), mensagem);
    }

    private async Task<bool> CarregarAsync(Guid id)
    {
        var detalhe = await consultar.DetalharAsync(await atorAtual.ObterAsync(), id, Cancelamento);
        if (detalhe is null)
        {
            return false;
        }

        Detalhe = detalhe;
        Hoje = calendario.DataLocal(relogio.AgoraUtc);
        return true;
    }
}
