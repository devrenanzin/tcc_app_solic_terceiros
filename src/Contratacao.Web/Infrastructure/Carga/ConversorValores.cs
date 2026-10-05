using System.Globalization;

namespace Contratacao.Web.Infrastructure.Carga;

/// <summary>Conversões dos valores da planilha QQP (seção 23).</summary>
internal static class ConversorValores
{
    private static readonly string[] NiveisRomanos = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII"];

    /// <summary>"R$ 12.345,67" vira 12345.67: tira o símbolo e o ponto de milhar e troca a vírgula por ponto.</summary>
    internal static decimal Reais(string texto)
    {
        var numero = texto.Replace("R$", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.')
            .Trim();

        return decimal.Parse(numero, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }

    /// <summary>Ordem do nível: I = 1 ... VIII = 8.</summary>
    internal static short OrdemNivel(string nivel)
    {
        var indice = Array.IndexOf(NiveisRomanos, nivel.Trim());
        return indice >= 0
            ? (short)(indice + 1)
            : throw new InvalidDataException($"Nível QQP desconhecido: {nivel}");
    }
}
