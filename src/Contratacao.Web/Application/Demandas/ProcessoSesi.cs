using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

// Processo da Gerenciadora (UC08–11), depois do aceite. A sequência obrigatória e as permissões ficam na Demanda;
// cada caso de uso carrega, executa a ação e grava. Datas de etapas concluídas não mudam (seção 12–15).

/// <summary>UC08 — Registrar a vaga com o link da plataforma externa; é um evento em Recrutamento.</summary>
internal sealed class RegistrarVaga(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, string linkExterno, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.RegistrarVaga(sesi, linkExterno, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC09 — Registrar o início das entrevistas; exige a vaga com link.</summary>
internal sealed class IniciarEntrevistas(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.IniciarEntrevistas(sesi, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC10 — Registrar o início dos exames médicos; exige a etapa Entrevistas.</summary>
internal sealed class IniciarExames(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.IniciarExames(sesi, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC11 — Finalizar a contratação; exige a etapa Exames Médicos e encerra o SLA.</summary>
internal sealed class FinalizarContratacao(IDemandas demandas, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator sesi, Guid demandaId, CancellationToken cancelamento)
    {
        var demanda = await Carregar.DemandaAsync(demandas, demandaId, cancelamento);
        demanda.Finalizar(sesi, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}
