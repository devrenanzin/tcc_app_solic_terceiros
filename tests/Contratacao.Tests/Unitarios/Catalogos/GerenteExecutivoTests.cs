using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Catalogos;

/// <summary>Gerentes executivos: cadastrados e desativados só pelo Admin (seções 19 e 23).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class GerenteExecutivoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Admin_cadastra_desativa_e_reativa()
    {
        var gerente = GerenteExecutivo.Cadastrar(_c.Admin, "  Gerência Fictícia  ");

        Assert.Equal("Gerência Fictícia", gerente.Nome);
        Assert.True(gerente.Ativo);

        gerente.AlterarSituacao(_c.Admin, false);
        Assert.False(gerente.Ativo);
        Assert.Throws<RegraNegocioException>(() => gerente.AlterarSituacao(_c.Admin, false));

        gerente.AlterarSituacao(_c.Admin, true);
        Assert.True(gerente.Ativo);
    }

    [Theory]
    [InlineData("Gestor")]
    [InlineData("Solicitante")]
    [InlineData("FuncionarioSesi")]
    public void So_o_admin_mantem_gerentes(string perfil)
    {
        var ator = Cenario.Novo(Enum.Parse<Perfil>(perfil));
        var gerente = GerenteExecutivo.Cadastrar(_c.Admin, "Gerência Fictícia");

        Assert.Throws<RegraNegocioException>(() => GerenteExecutivo.Cadastrar(ator, "Outra"));
        Assert.Throws<RegraNegocioException>(() => gerente.AlterarSituacao(ator, false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_e_obrigatorio(string nome)
        => Assert.Throws<RegraNegocioException>(() => GerenteExecutivo.Cadastrar(_c.Admin, nome));
}
