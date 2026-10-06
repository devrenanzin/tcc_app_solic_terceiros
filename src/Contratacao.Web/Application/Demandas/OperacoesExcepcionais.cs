using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>
/// UC18 — Cancelar demanda: Gestor do contrato, ou o Admin em caráter excepcional (UC20), sempre com
/// justificativa. Nada é excluído; o histórico e o log de auditoria registram quem, quando e por quê.
/// </summary>
internal sealed class CancelarDemanda(
    IDemandas demandas, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio, Notificacoes.Notificador notificador)
{
    internal async Task ExecutarAsync(Ator ator, Guid demandaId, string justificativa, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        var situacao = $"{demanda.Etapa} / {demanda.Status}";

        demanda.Cancelar(ator, justificativa, relogio);
        auditoria.Registrar(LogAuditoria.De(ator, nameof(Demanda), demanda.Id,
            ator.Eh(Perfil.Admin) ? "OperacaoExcepcional.Cancelamento" : "Cancelamento",
            situacao, $"{demanda.Etapa} / {demanda.Status}", demanda.MotivoCancelamento, relogio.AgoraUtc));
        await notificador.RegistrarAsync(demanda, Domain.Notificacoes.TipoNotificacao.DemandaCancelada, cancelamento);
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC20 — Transição forçada pelo Admin, para trás ou para frente, entre Recrutamento e Exames Médicos.</summary>
internal sealed class ForcarTransicao(IDemandas demandas, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator admin, Guid demandaId, Etapa destino, string justificativa, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        var anterior = demanda.Etapa;

        demanda.ForcarTransicao(admin, destino, justificativa, relogio);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(Demanda), demanda.Id, "OperacaoExcepcional.Transicao",
            anterior.ToString(), demanda.Etapa.ToString(), justificativa.Trim(), relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC20 — Alteração excepcional de data congelada pelo Admin, com justificativa.</summary>
internal sealed class AlterarDataExcepcional(
    IDemandas demandas, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio, ICalendarioSla calendario)
{
    internal async Task ExecutarAsync(
        Ator admin, Guid demandaId, DataAlteravel data, Guid? passagemId, DateTime novaUtc, string justificativa, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);

        var (campo, anterior, novo) = demanda.AlterarDataExcepcional(admin, data, passagemId, novaUtc, justificativa, relogio, calendario);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(Demanda), demanda.Id, "OperacaoExcepcional.AlteracaoData",
            $"{campo}: {anterior}", $"{campo}: {novo}", justificativa.Trim(), relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}
