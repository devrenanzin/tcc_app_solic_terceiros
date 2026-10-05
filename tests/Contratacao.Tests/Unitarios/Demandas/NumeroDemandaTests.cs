using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Formato AAAA-NNNNNN (UC02, RF40).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class NumeroDemandaTests
{
    [Theory]
    [InlineData(2026, 123, "2026-000123")]
    [InlineData(2026, 1, "2026-000001")]
    [InlineData(2027, 999_999, "2027-999999")]
    public void Formata_ano_e_sequencial_de_seis_digitos(int ano, int sequencial, string esperado)
    {
        var numero = NumeroDemanda.Formatar(ano, sequencial);

        Assert.Equal(esperado, numero);
        Assert.True(NumeroDemanda.EhValido(numero));
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 1_000_000)]
    [InlineData(1999, 1)]
    public void Recusa_ano_ou_sequencial_fora_da_faixa(int ano, int sequencial)
        => Assert.Throws<RegraNegocioException>(() => NumeroDemanda.Formatar(ano, sequencial));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026000123")]
    [InlineData("2026-00012A")]
    [InlineData("2026_000123")]
    [InlineData("2026-0001234")]
    public void Reconhece_numero_invalido(string? numero)
        => Assert.False(NumeroDemanda.EhValido(numero));
}
