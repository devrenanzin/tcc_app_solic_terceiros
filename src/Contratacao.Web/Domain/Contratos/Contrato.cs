using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Contratos;

/// <summary>Contrato de uma Contratada. Número começa com 59 e tem até 10 dígitos (seção 23).</summary>
internal sealed class Contrato
{
    private Contrato() { } // EF Core

    internal Guid Id { get; private set; }
    internal Guid ContratadaId { get; private set; }
    internal string Numero { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }

    internal static Contrato Criar(Guid id, Guid contratadaId, string numero)
    {
        if (!NumeroValido(numero))
        {
            throw new RegraNegocioException("O número do contrato começa com 59 e tem até 10 dígitos.");
        }

        return new Contrato { Id = id, ContratadaId = contratadaId, Numero = numero, Ativo = true };
    }

    internal static bool NumeroValido(string? numero)
        => numero is { Length: >= 2 and <= 10 }
        && numero.StartsWith("59", StringComparison.Ordinal)
        && numero.All(char.IsAsciiDigit);
}
