using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Usuarios;

/// <summary>Regras da senha própria do sistema (RN11). A senha nunca é gravada, só o hash.</summary>
internal static class Senha
{
    // De 8 a 128 caracteres (Cliente, revisão de 05/10/2026).
    internal const int TamanhoMinimo = 8;
    internal const int TamanhoMaximo = 128;

    internal static void Validar(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < TamanhoMinimo || senha.Length > TamanhoMaximo)
        {
            throw new RegraNegocioException($"A senha precisa ter de {TamanhoMinimo} a {TamanhoMaximo} caracteres.");
        }
    }
}
