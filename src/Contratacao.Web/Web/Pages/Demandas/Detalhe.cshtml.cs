using System.Security.Claims;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC03 e UC15 — Consultar a demanda; daqui o Gestor do contrato aprova ou devolve (UC04, UC05) e a Gerenciadora do
/// contrato aceita ou devolve (UC05, UC07) e conduz o processo até a contratação (UC08–11). Só aparecem as
/// ações que o domínio permite ao usuário.
/// </summary>
internal sealed class DetalheModel(
    ConsultarDemandas consultar,
    AprovarDemanda aprovar,
    DevolverDemandaPeloGestor devolverPeloGestor,
    DevolverDemandaPeloSesi devolverPeloSesi,
    AceitarDemanda aceitar,
    RegistrarVaga registrarVaga,
    IniciarEntrevistas iniciarEntrevistas,
    IniciarExames iniciarExames,
    FinalizarContratacao finalizar,
    CancelarDemanda cancelar,
    ForcarTransicao forcarTransicao,
    AlterarDataExcepcional alterarData,
    AtorAtual atorAtual,
    IRelogio relogio,
    ICalendarioSla calendario) : PaginaBase
{
    internal DetalheDemanda Detalhe { get; private set; } = null!;
    internal DateOnly Hoje { get; private set; }
    internal DateTime Agora { get; private set; }

    /// <summary>Chave do rascunho deste usuário, para apagá-lo logo depois do envio.</summary>
    internal string? RascunhoParaLimpar { get; private set; }

    [BindProperty]
    public string? Motivo { get; set; }

    [BindProperty]
    public TipoInconsistencia? Tipo { get; set; }

    [BindProperty]
    public string? LinkVaga { get; set; }

    [BindProperty]
    public string? Justificativa { get; set; }

    [BindProperty]
    public Etapa? EtapaDestino { get; set; }

    /// <summary>Data escolhida para a operação excepcional: "InicioEtapa:{id}", "ConclusaoEtapa:{id}", "AberturaVaga" ou "InicioSla".</summary>
    [BindProperty]
    public string? DataAlvo { get; set; }

    /// <summary>Nova data e hora, digitada no horário de Brasília.</summary>
    [BindProperty]
    public DateTime? NovaData { get; set; }

    /// <summary>O Admin vê o painel de operações excepcionais (UC20).</summary>
    internal bool OperacoesExcepcionais { get; private set; }

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
            "Demanda aprovada. Ela segue para a Validação da Gerenciadora e o prazo de SLA começou.");

    public Task<IActionResult> OnPostDevolverGestorAsync(Guid id)
        => ExecutarAsync(async ator => await devolverPeloGestor.ExecutarAsync(ator, id, Motivo ?? string.Empty, Cancelamento),
            "Demanda devolvida ao Solicitante para correção.");

    public Task<IActionResult> OnPostAceitarAsync(Guid id)
        => ExecutarAsync(async ator => await aceitar.ExecutarAsync(ator, id, Cancelamento),
            "Demanda aceita. Você é o responsável da Gerenciadora e ela está em Recrutamento.");

    public Task<IActionResult> OnPostDevolverSesiAsync(Guid id)
        => ExecutarAsync(async ator =>
        {
            if (Tipo is not { } tipo)
            {
                throw new RegraNegocioException("Informe o tipo da inconsistência.");
            }

            await devolverPeloSesi.ExecutarAsync(ator, id, tipo, Motivo ?? string.Empty, Cancelamento);
        }, "Demanda devolvida ao Solicitante para correção.");

    public Task<IActionResult> OnPostRegistrarVagaAsync(Guid id)
        => ExecutarAsync(async ator => await registrarVaga.ExecutarAsync(ator, id, LinkVaga ?? string.Empty, Cancelamento),
            "Vaga registrada com o link da plataforma externa.");

    public Task<IActionResult> OnPostIniciarEntrevistasAsync(Guid id)
        => ExecutarAsync(async ator => await iniciarEntrevistas.ExecutarAsync(ator, id, Cancelamento),
            "Entrevistas iniciadas. A etapa Recrutamento foi concluída.");

    public Task<IActionResult> OnPostIniciarExamesAsync(Guid id)
        => ExecutarAsync(async ator => await iniciarExames.ExecutarAsync(ator, id, Cancelamento),
            "Exames médicos iniciados. A etapa Entrevistas foi concluída.");

    public Task<IActionResult> OnPostFinalizarAsync(Guid id)
        => ExecutarAsync(async ator => await finalizar.ExecutarAsync(ator, id, Cancelamento),
            "Contratação finalizada. O prazo de SLA foi encerrado.");

    /// <summary>UC18 pelo Gestor do contrato; pelo Admin é operação excepcional (UC20).</summary>
    public Task<IActionResult> OnPostCancelarAsync(Guid id)
        => ExecutarAsync(async ator => await cancelar.ExecutarAsync(ator, id, Justificativa ?? string.Empty, Cancelamento),
            "Demanda cancelada. O histórico continua disponível; nada foi excluído.");

    public Task<IActionResult> OnPostForcarTransicaoAsync(Guid id)
        => ExecutarAsync(async ator =>
        {
            if (EtapaDestino is not { } destino)
            {
                throw new RegraNegocioException("Escolha a etapa de destino.");
            }

            await forcarTransicao.ExecutarAsync(ator, id, destino, Justificativa ?? string.Empty, Cancelamento);
        }, "Etapa alterada por operação excepcional, com registro na auditoria.");

    public Task<IActionResult> OnPostAlterarDataAsync(Guid id)
        => ExecutarAsync(async ator =>
        {
            var partes = (DataAlvo ?? string.Empty).Split(':');
            if (!Enum.TryParse<DataAlteravel>(partes[0], out var data) || NovaData is not { } nova)
            {
                throw new RegraNegocioException("Escolha a data a alterar e informe a nova data e hora.");
            }

            Guid? passagem = partes.Length > 1 && Guid.TryParse(partes[1], out var passagemId) ? passagemId : null;
            await alterarData.ExecutarAsync(ator, id, data, passagem, Formatacao.ParaUtc(nova), Justificativa ?? string.Empty, Cancelamento);
        }, "Data alterada por operação excepcional, com registro na auditoria.");

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
        var ator = await atorAtual.ObterAsync();
        var detalhe = await consultar.DetalharAsync(ator, id, Cancelamento);
        if (detalhe is null)
        {
            return false;
        }

        Detalhe = detalhe;
        OperacoesExcepcionais = ator.Eh(Domain.Usuarios.Perfil.Admin);
        Agora = relogio.AgoraUtc;
        Hoje = calendario.DataLocal(Agora);
        return true;
    }
}
