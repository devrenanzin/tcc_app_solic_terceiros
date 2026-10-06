using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>
/// Filtros da tela de acompanhamento (seção 21–22): status, etapa, contratada, Gestor, Solicitante, período
/// (data de envio; Cliente), farol e responsável da Gerenciadora, além do quadro do painel e da situação geral.
/// </summary>
internal sealed record FiltroDemandas
{
    /// <summary>Quadro do painel; quando informado, vale no lugar da situação.</summary>
    internal Quadro? Quadro { get; init; }

    /// <summary>Padrão: só as em andamento (Cliente, revisão de 06/10/2026).</summary>
    internal Situacao Situacao { get; init; } = Situacao.EmAndamento;

    internal Etapa? Etapa { get; init; }
    internal StatusDemanda? Status { get; init; }
    internal Farol? Farol { get; init; }
    internal Guid? ContratadaId { get; init; }
    internal Guid? GestorId { get; init; }
    internal Guid? SolicitanteId { get; init; }
    internal Guid? ResponsavelSesiId { get; init; }

    /// <summary>Data de envio, no horário de Brasília.</summary>
    internal DateOnly? EnviadaDe { get; init; }

    internal DateOnly? EnviadaAte { get; init; }

    /// <summary>Parte do número da demanda.</summary>
    internal string? Numero { get; init; }

    internal bool Aceita(ResumoDemanda r, Ator ator, ICalendarioSla calendario)
    {
        var enviada = calendario.DataLocal(r.DataEnvio);
        return (Quadro is { } quadro ? Quadros.Inclui(quadro, r.Posicao, ator) : Quadros.Inclui(Situacao, r.Posicao))
            && (Etapa is not { } etapa || r.Etapa == etapa)
            && (Status is not { } status || r.Status == status)
            && (Farol is not { } farol || r.Farol == farol)
            && (ContratadaId is not { } contratada || r.ContratadaId == contratada)
            && (GestorId is not { } gestor || r.GestorId == gestor)
            && (SolicitanteId is not { } solicitante || r.SolicitanteId == solicitante)
            && (ResponsavelSesiId is not { } responsavel || r.ResponsavelSesiId == responsavel)
            && (EnviadaDe is not { } de || enviada >= de)
            && (EnviadaAte is not { } ate || enviada <= ate)
            && (string.IsNullOrWhiteSpace(Numero) || r.Numero.Contains(Numero.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
