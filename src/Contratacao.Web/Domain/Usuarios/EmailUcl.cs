using System.Text.RegularExpressions;
using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Usuarios;

/// <summary>RN11: todo usuário usa um e-mail do domínio @ucl.br.</summary>
internal static partial class EmailUcl
{
    internal const string Dominio = "@ucl.br";

    /// <summary>Valida e devolve o e-mail em minúsculas, sem espaços.</summary>
    internal static string Normalizar(string? email)
    {
        var normalizado = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!Formato().IsMatch(normalizado))
        {
            throw new RegraNegocioException("Use um e-mail válido do domínio @ucl.br.");
        }

        return normalizado;
    }

    // Parte local de 1 a 64 caracteres (RFC 5321), seguida de @ucl.br.
    [GeneratedRegex(@"^[a-z0-9._%+\-]{1,64}@ucl\.br$", RegexOptions.CultureInvariant)]
    private static partial Regex Formato();
}
