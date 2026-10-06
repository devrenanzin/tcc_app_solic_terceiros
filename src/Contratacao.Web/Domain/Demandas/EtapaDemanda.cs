using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Uma passagem da demanda por uma etapa. A data de conclusão é imutável depois de preenchida (seção 12–15).</summary>
internal sealed class EtapaDemanda
{
    private EtapaDemanda() { } // EF Core

    private EtapaDemanda(Etapa etapa, StatusDemanda status, DateTime inicioUtc)
    {
        // Id gerado no domínio: a passagem é escolhida pelo id nas operações excepcionais (UC20).
        Id = Guid.CreateVersion7();
        Etapa = etapa;
        Status = status;
        DataInicio = inicioUtc;
    }

    internal Guid Id { get; private set; }
    internal Etapa Etapa { get; private set; }
    internal StatusDemanda Status { get; private set; }
    internal Guid? UsuarioResponsavelId { get; private set; }
    internal DateTime DataInicio { get; private set; }
    internal DateTime? DataConclusao { get; private set; }
    internal string? Observacao { get; private set; }
    internal bool Aberta => DataConclusao is null;

    internal static EtapaDemanda Iniciar(Etapa etapa, StatusDemanda status, DateTime inicioUtc)
        => new(etapa, status, inicioUtc);

    internal void AtualizarStatus(StatusDemanda status)
    {
        GarantirAberta();
        Status = status;
    }

    /// <summary>Só na passagem aberta: etapa concluída não muda (congelamento, seção 12–15).</summary>
    internal void DefinirResponsavel(Guid usuarioId)
    {
        GarantirAberta();
        UsuarioResponsavelId = usuarioId;
    }

    internal void Encerrar(StatusDemanda statusFinal, DateTime conclusaoUtc)
    {
        GarantirAberta();
        Status = statusFinal;
        DataConclusao = conclusaoUtc;
    }

    /// <summary>
    /// Alteração excepcional de data pelo Admin (UC20), chamada só pela Demanda: única forma de mudar uma data
    /// congelada. A conclusão nunca fica antes do início.
    /// </summary>
    internal void CorrigirDatas(DateTime inicioUtc, DateTime? conclusaoUtc)
    {
        if (conclusaoUtc < inicioUtc)
        {
            throw new RegraNegocioException("A conclusão da etapa não pode ser anterior ao início.");
        }

        if (conclusaoUtc is null && !Aberta)
        {
            throw new RegraNegocioException("Etapa concluída precisa de data de conclusão.");
        }

        DataInicio = inicioUtc;
        DataConclusao = conclusaoUtc;
    }

    private void GarantirAberta()
    {
        if (!Aberta)
        {
            throw new RegraNegocioException("A etapa já foi concluída e sua data não pode ser alterada.");
        }
    }
}
