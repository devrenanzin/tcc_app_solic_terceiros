using Contratacao.Web.Application.Notificacoes;
using Contratacao.Web.Domain.Notificacoes;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages;

/// <summary>Avisos do usuário (seção 29): os mais recentes primeiro; abrir marca como lido e leva à demanda.</summary>
internal sealed class NotificacoesModel(ConsultarNotificacoes consultar, AtorAtual atorAtual) : PaginaBase
{
    internal IReadOnlyList<Notificacao> Avisos { get; private set; } = [];

    public async Task OnGetAsync() => Avisos = await consultar.ListarAsync(await atorAtual.ObterAsync(), Cancelamento);

    public async Task<IActionResult> OnPostAbrirAsync(Guid id)
    {
        Guid? demanda = null;
        try
        {
            demanda = await consultar.AbrirAsync(await atorAtual.ObterAsync(), id, Cancelamento);
        }
        catch (Domain.Comum.RegraNegocioException erro)
        {
            TempData[ChaveErro] = erro.Message;
        }

        return demanda is { } d ? Redirect($"/Demandas/Detalhe/{d}") : RedirectToPage();
    }

    public async Task<IActionResult> OnPostLerTodasAsync()
        => await ExecutarERecarregarAsync(
            async () => await consultar.MarcarTodasLidasAsync(await atorAtual.ObterAsync(), Cancelamento),
            "Todos os avisos foram marcados como lidos.");

    internal static string Icone(TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.NovaDemanda => "bi-inbox",
        TipoNotificacao.DemandaAprovada => "bi-check2-circle",
        TipoNotificacao.CorrecaoSolicitada => "bi-arrow-return-left",
        TipoNotificacao.AceitaPelaGerenciadora => "bi-person-check",
        TipoNotificacao.ProximaDoVencimento => "bi-exclamation-triangle",
        TipoNotificacao.PrazoVencido => "bi-alarm",
        TipoNotificacao.ContratacaoFinalizada => "bi-check2-all",
        TipoNotificacao.DemandaCancelada => "bi-x-circle",
        _ => "bi-bell",
    };
}
