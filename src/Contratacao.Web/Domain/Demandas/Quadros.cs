using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Quadros dos painéis por perfil (seções 21–22).</summary>
internal enum Quadro
{
    MinhasDemandas = 1,
    EmAndamento = 2,
    AguardandoCorrecao = 3,
    Finalizadas = 4,
    Canceladas = 5,
    AguardandoValidacao = 6,
    CorrecoesPendentes = 7,
    Aprovadas = 8,
    EmProcessoSesi = 9,
    ProximasDoVencimento = 10,
    Atrasadas = 11,
    AguardandoAceite = 12,
    Recrutamento = 13,
    Entrevistas = 14,
    ExamesMedicos = 15,
}

/// <summary>Situação geral usada no filtro das listas: o padrão é ver só as que estão em andamento (Cliente).</summary>
internal enum Situacao
{
    EmAndamento = 1,
    Concluidas = 2,
    Canceladas = 3,
    Todas = 4,
}

/// <summary>O que os quadros e o filtro de situação precisam saber de uma demanda.</summary>
internal sealed record PosicaoDemanda(Etapa Etapa, StatusDemanda Status, Farol Farol, Guid ContratoId);

/// <summary>
/// Regras dos quadros (seções 21–22): quais quadros cada perfil vê e quais demandas entram em cada um.
/// "Próximas do vencimento" = farol amarelo ou laranja; "Atrasadas" = vermelho, não finalizadas.
/// </summary>
internal static class Quadros
{
    internal static IReadOnlyList<Quadro> DoPerfil(Perfil perfil) => perfil switch
    {
        Perfil.Solicitante => [Quadro.MinhasDemandas, Quadro.EmAndamento, Quadro.AguardandoCorrecao, Quadro.Finalizadas, Quadro.Canceladas],
        Perfil.Gestor =>
        [
            Quadro.AguardandoValidacao, Quadro.CorrecoesPendentes, Quadro.Aprovadas, Quadro.EmProcessoSesi,
            Quadro.ProximasDoVencimento, Quadro.Atrasadas,
        ],
        Perfil.FuncionarioSesi =>
        [
            Quadro.AguardandoAceite, Quadro.Recrutamento, Quadro.Entrevistas, Quadro.ExamesMedicos, Quadro.Finalizadas,
            Quadro.ProximasDoVencimento, Quadro.Atrasadas,
        ],
        // Admin: visão geral de todas as demandas.
        _ => [Quadro.EmAndamento, Quadro.ProximasDoVencimento, Quadro.Atrasadas, Quadro.Finalizadas, Quadro.Canceladas],
    };

    /// <summary>
    /// Se a demanda entra no quadro, para quem a vê. A visibilidade (seção 4) é aplicada antes; aqui só a regra do
    /// quadro. Do painel do Gestor, só "Aguardando validação" se limita aos contratos dele (Cliente, revisão de 06/10/2026).
    /// </summary>
    internal static bool Inclui(Quadro quadro, PosicaoDemanda d, Ator ator) => quadro switch
    {
        Quadro.MinhasDemandas => true,
        Quadro.EmAndamento => Ativa(d) && d.Status != StatusDemanda.AguardandoCorrecao,
        Quadro.AguardandoCorrecao or Quadro.CorrecoesPendentes => d.Status == StatusDemanda.AguardandoCorrecao,
        Quadro.Finalizadas => d.Status == StatusDemanda.Concluido,
        Quadro.Canceladas => d.Status == StatusDemanda.Cancelado,
        Quadro.AguardandoValidacao => d is { Etapa: Etapa.ValidacaoGestor, Status: StatusDemanda.EmAnalise } && ator.AtuaNoContrato(d.ContratoId),
        // "Aprovadas": aprovadas pelo Gestor e ainda à espera do aceite do SESI (Cliente).
        Quadro.Aprovadas or Quadro.AguardandoAceite => d is { Etapa: Etapa.ValidacaoSesi, Status: StatusDemanda.AguardandoResponsavel },
        Quadro.EmProcessoSesi => d.Status == StatusDemanda.EmAndamento,
        Quadro.Recrutamento => d is { Etapa: Etapa.Recrutamento, Status: StatusDemanda.EmAndamento },
        Quadro.Entrevistas => d is { Etapa: Etapa.Entrevistas, Status: StatusDemanda.EmAndamento },
        Quadro.ExamesMedicos => d is { Etapa: Etapa.ExamesMedicos, Status: StatusDemanda.EmAndamento },
        Quadro.ProximasDoVencimento => Ativa(d) && d.Farol is Farol.Amarelo or Farol.Laranja,
        Quadro.Atrasadas => Ativa(d) && d.Farol == Farol.Vermelho,
        _ => false,
    };

    internal static bool Inclui(Situacao situacao, PosicaoDemanda d) => situacao switch
    {
        Situacao.EmAndamento => Ativa(d),
        Situacao.Concluidas => d.Status == StatusDemanda.Concluido,
        Situacao.Canceladas => d.Status == StatusDemanda.Cancelado,
        _ => true,
    };

    private static bool Ativa(PosicaoDemanda d) => d.Status is not (StatusDemanda.Concluido or StatusDemanda.Cancelado);
}
