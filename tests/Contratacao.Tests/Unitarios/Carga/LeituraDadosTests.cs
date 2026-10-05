using Contratacao.Web.Infrastructure.Carga;

namespace Contratacao.Tests.Unitarios.Carga;

/// <summary>Leitura dos CSV e conversão dos valores da planilha QQP (seção 23).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class LeituraDadosTests
{
    [Theory]
    [InlineData("R$ 12.345,67", "12345.67")]
    [InlineData("R$ 444,35", "444.35")]
    [InlineData("R$ 1.234.567,00", "1234567.00")]
    public void Reais_sem_simbolo_e_com_ponto_decimal(string texto, string esperado)
        => Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), ConversorValores.Reais(texto));

    [Theory]
    [InlineData("I", 1)]
    [InlineData("IV", 4)]
    [InlineData("VIII", 8)]
    public void Nivel_romano_vira_ordem(string nivel, short ordem)
        => Assert.Equal(ordem, ConversorValores.OrdemNivel(nivel));

    [Fact]
    public void Nivel_desconhecido_e_recusado()
        => Assert.Throws<InvalidDataException>(() => ConversorValores.OrdemNivel("IX"));

    [Fact]
    public void Separa_campos_entre_aspas_com_virgula_dentro()
    {
        var campos = LeitorCsv.Separar("\"1\",\"QQP SUDESTE\",\"R$ 12.345,67\",\"diz \"\"oi\"\"\"");

        Assert.Equal(["1", "QQP SUDESTE", "R$ 12.345,67", "diz \"oi\""], campos);
    }
}
