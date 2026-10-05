using Contratacao.Web.Domain.Comum;

namespace Contratacao.Tests.Unitarios.Apoio;

/// <summary>Relógio controlado pelo teste.</summary>
internal sealed class RelogioFixo(DateTime agoraUtc) : IRelogio
{
    public DateTime AgoraUtc { get; private set; } = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);

    internal void Avancar(TimeSpan intervalo) => AgoraUtc += intervalo;

    internal void Definir(DateTime agoraUtc) => AgoraUtc = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);
}
