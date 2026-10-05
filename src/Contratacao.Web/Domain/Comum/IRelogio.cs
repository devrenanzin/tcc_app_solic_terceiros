namespace Contratacao.Web.Domain.Comum;

/// <summary>Fonte da data e hora atual. O domínio nunca usa DateTime.Now (RNF09).</summary>
internal interface IRelogio
{
    /// <summary>Instante atual em UTC.</summary>
    DateTime AgoraUtc { get; }
}
