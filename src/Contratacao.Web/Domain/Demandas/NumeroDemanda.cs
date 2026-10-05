using System.Globalization;
using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Número AAAA-NNNNNN gerado no envio (UC02, RF40). O sequencial vem de SequenciaNumeroDemanda.</summary>
internal static class NumeroDemanda
{
    private const int MaiorSequencial = 999_999;

    internal static string Formatar(int ano, int sequencial)
    {
        if (ano is < 2000 or > 9999)
        {
            throw new RegraNegocioException("Ano inválido para o número da demanda.");
        }

        if (sequencial is < 1 or > MaiorSequencial)
        {
            throw new RegraNegocioException("O sequencial da demanda precisa ter de 1 a 6 dígitos.");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{ano:D4}-{sequencial:D6}");
    }

    internal static bool EhValido(string? numero)
        => numero is { Length: 11 }
        && numero[4] == '-'
        && numero.AsSpan(0, 4).ContainsAnyExceptInRange('0', '9') is false
        && numero.AsSpan(5).ContainsAnyExceptInRange('0', '9') is false
        && numero.AsSpan(5) is not "000000";
}
