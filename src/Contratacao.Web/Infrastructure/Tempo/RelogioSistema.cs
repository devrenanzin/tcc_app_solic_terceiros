using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Infrastructure.Tempo;

/// <summary>Relógio real da aplicação; os testes usam um relógio fixo.</summary>
internal sealed class RelogioSistema(TimeProvider tempo) : IRelogio
{
    public DateTime AgoraUtc => tempo.GetUtcNow().UtcDateTime;
}
