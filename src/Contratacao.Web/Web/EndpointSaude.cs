namespace Contratacao.Web.Web;

internal sealed record RespostaSaude(string Status);

internal static class EndpointSaude
{
    internal static RespostaSaude Obter() => new("ok");
}
