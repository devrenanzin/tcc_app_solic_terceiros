namespace Contratacao.Web.Domain.Contratos;

/// <summary>Empresa que fornece os funcionários. Hoje só a Gerenciadora; CNPJ opcional (revisão de 05/10/2026).</summary>
internal sealed class Contratada
{
    private Contratada() { } // EF Core

    internal Guid Id { get; private set; }
    internal string RazaoSocial { get; private set; } = string.Empty;
    internal string? NomeFantasia { get; private set; }
    internal string? Cnpj { get; private set; }
    internal bool Ativo { get; private set; }
}
