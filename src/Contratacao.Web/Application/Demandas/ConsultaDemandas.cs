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
