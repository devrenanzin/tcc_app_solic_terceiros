using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

// Validação do Gestor (UC04, UC05) e da Gerenciadora (UC05, UC07). As regras ficam na Demanda; cada caso de uso
// carrega, executa a ação e grava.

internal sealed class AprovarDemanda(
    IDemandas demandas, IParametros parametros, IUnidadeDeTrabalho unidade, IRelogio relogio, ICalendarioSla calendario)
{
    internal async Task ExecutarAsync(Ator gestor, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        var prazo = await parametros.PrazoSlaDiasAsync(cancelamento);

        demanda.Aprovar(gestor, prazo, relogio, calendario);
        await unidade.SalvarAsync(cancelamento);
    }
}

internal sealed class DevolverDemandaPeloGestor(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator gestor, Guid demandaId, string motivo, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.DevolverPeloGestor(gestor, motivo, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

internal sealed class DevolverDemandaPeloSesi(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, TipoInconsistencia tipo, string motivo, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.DevolverPeloSesi(sesi, tipo, motivo, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

internal sealed class AceitarDemanda(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.Aceitar(sesi, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

internal static class Carregar
{
    internal static async Task<Demanda> DemandaAsync(IDemandas demandas, Guid id, CancellationToken cancelamento)
        => await demandas.ObterAsync(id, cancelamento) ?? throw new RegraNegocioException("Demanda não encontrada.");
}
