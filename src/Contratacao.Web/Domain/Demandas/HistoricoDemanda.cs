using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Registro imutável do fluxo da demanda (seção 12–15): quem, com qual perfil, de onde, quando e o que mudou.</summary>
internal sealed class HistoricoDemanda
{
    private HistoricoDemanda() { } // EF Core

    internal HistoricoDemanda(
        EventoDemanda evento,
        Ator ator,
        DateTime dataHoraUtc,
        Etapa? etapaAnterior,
        StatusDemanda? statusAnterior,
        Etapa etapaNova,
        StatusDemanda statusNovo,
        string? observacao)
    {
        Evento = evento;
        UsuarioId = ator.Id;
        PerfilUsuario = ator.Perfil;
        EnderecoIp = ator.EnderecoIp;
        DataHora = dataHoraUtc;
        EtapaAnterior = etapaAnterior;
        StatusAnterior = statusAnterior;
        EtapaNova = etapaNova;
        StatusNovo = statusNovo;
        Observacao = observacao;
    }

    internal Guid Id { get; private set; }
    internal EventoDemanda Evento { get; private set; }
    internal Guid UsuarioId { get; private set; }
    internal Perfil PerfilUsuario { get; private set; }
    internal string? EnderecoIp { get; private set; }
    internal DateTime DataHora { get; private set; }
    internal Etapa? EtapaAnterior { get; private set; }
    internal StatusDemanda? StatusAnterior { get; private set; }
    internal Etapa EtapaNova { get; private set; }
    internal StatusDemanda StatusNovo { get; private set; }
    internal string? Observacao { get; private set; }
}
