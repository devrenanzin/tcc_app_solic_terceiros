namespace Contratacao.Web.Infrastructure.Carga;

/// <summary>Admin inicial, lido da configuração (user-secrets); nunca fixo no código (seção 23).</summary>
internal sealed class OpcoesAdminInicial
{
    internal const string Secao = "AdminInicial";

    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
}
