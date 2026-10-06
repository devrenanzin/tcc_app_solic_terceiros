using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>UC03 e UC15 — Consultar e acompanhar demandas, no escopo de cada perfil (seção 4).</summary>
internal sealed class ConsultarDemandas(
    IDemandas demandas,
    ICatalogosDemanda catalogos,
    IAnexos anexos,
    IArmazenamentoArquivos armazenamento,
    IRelogio relogio,
    ICalendarioSla calendario)
{
    /// <summary>Todas as demandas que o ator vê, com o farol de hoje.</summary>
    internal async Task<IReadOnlyList<ResumoDemanda>> ListarAsync(Ator ator, CancellationToken cancelamento)
    {
        var hoje = calendario.DataLocal(relogio.AgoraUtc);
        var resumos = await demandas.ListarAsync(FiltroVisibilidade.Para(ator), cancelamento);

        return [.. resumos.Select(r => r with
        {
            Farol = RegraFarol.Calcular(
                r.Sla,
                r.Status == StatusDemanda.Cancelado,
                r.DataFinalizacao is { } final ? calendario.DataLocal(final) : null,
                hoje),
        })];
    }

    /// <summary>
    /// Tela de acompanhamento (seções 21–22): as demandas visíveis que passam no quadro escolhido ou nos filtros.
    /// Sem quadro, a situação padrão é "em andamento" (Cliente); o período é o da data de envio (Cliente).
    /// </summary>
    internal async Task<IReadOnlyList<ResumoDemanda>> FiltrarAsync(Ator ator, FiltroDemandas filtro, CancellationToken cancelamento)
    {
        var todas = await ListarAsync(ator, cancelamento);
        return [.. todas.Where(r => filtro.Aceita(r, ator, calendario))];
    }

    /// <summary>Quantas demandas há em cada quadro do painel do perfil (seções 21–22).</summary>
    internal async Task<IReadOnlyList<(Quadro Quadro, int Quantidade)>> PainelAsync(Ator ator, CancellationToken cancelamento)
    {
        var todas = await ListarAsync(ator, cancelamento);
        return [.. Quadros.DoPerfil(ator.Perfil).Select(q => (q, todas.Count(r => Quadros.Inclui(q, r.Posicao, ator))))];
    }

    /// <summary>A demanda com nomes, anexos, farol e as ações que o ator pode executar; nulo se ele não puder vê-la.</summary>
    internal async Task<DetalheDemanda?> DetalharAsync(Ator ator, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await demandas.ObterAsync(demandaId, cancelamento);
        if (demanda is null || !FiltroVisibilidade.Para(ator).Inclui(demanda.UsuarioSolicitanteId, demanda.ContratoId))
        {
            return null;
        }

        return new DetalheDemanda(
            demanda,
            await catalogos.DescreverAsync(demanda, cancelamento),
            await anexos.DaDemandaAsync(demanda.Id, cancelamento),
            demanda.ObterFarol(relogio, calendario),
            demanda.AcoesDisponiveis(ator));
    }

    /// <summary>Arquivo anexado, para quem pode ver a demanda.</summary>
    internal async Task<(Anexo Anexo, Func<Stream> Abrir)?> AnexoAsync(
        Ator ator, Guid anexoId, CancellationToken cancelamento)
    {
        var anexo = await anexos.ObterAsync(anexoId, cancelamento);
        var demanda = anexo is null ? null : await demandas.ObterAsync(anexo.DemandaId, cancelamento);
        if (anexo is null || demanda is null || !FiltroVisibilidade.Para(ator).Inclui(demanda.UsuarioSolicitanteId, demanda.ContratoId))
        {
            return null;
        }

        return (anexo, () => armazenamento.Abrir(anexo.Identificador));
    }
}
