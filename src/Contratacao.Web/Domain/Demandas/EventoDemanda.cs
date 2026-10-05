namespace Contratacao.Web.Domain.Demandas;

/// <summary>Eventos da linha do tempo (seção 5.3).</summary>
internal enum EventoDemanda
{
    Enviada = 1,
    AprovadaPeloGestor = 2,
    DevolvidaParaCorrecao = 3,
    CorrecaoEnviada = 4,
    AceitaPeloSesi = 5,
    VagaAberta = 6,
    EntrevistasIniciadas = 7,
    ExamesIniciados = 8,
    ContratacaoFinalizada = 9,
    Cancelada = 10,
}
