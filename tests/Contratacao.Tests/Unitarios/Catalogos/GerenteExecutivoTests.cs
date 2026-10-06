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
        var gerente = GerenteExecutivo.Cadastrar(_c.Admin, "  Gerência Fictícia  ", [Cenario.CorredorNorte.Id]);

        Assert.Equal("Gerência Fictícia", gerente.Nome);
        Assert.True(gerente.AtendeCorredor(Cenario.CorredorNorte.Id));
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
        var gerente = GerenteExecutivo.Cadastrar(_c.Admin, "Gerência Fictícia", [Cenario.CorredorNorte.Id]);

        Assert.Throws<RegraNegocioException>(() => GerenteExecutivo.Cadastrar(ator, "Outra", [Cenario.CorredorNorte.Id]));
        Assert.Throws<RegraNegocioException>(() => gerente.DefinirCorredores(ator, [Cenario.CorredorSudeste.Id]));
        Assert.Throws<RegraNegocioException>(() => gerente.AlterarSituacao(ator, false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_e_obrigatorio(string nome)
        => Assert.Throws<RegraNegocioException>(() => GerenteExecutivo.Cadastrar(_c.Admin, nome, [Cenario.CorredorNorte.Id]));

    [Fact]
    public void Gerente_atende_ao_menos_um_corredor_e_o_admin_troca_os_corredores()
    {
        Assert.Throws<RegraNegocioException>(() => GerenteExecutivo.Cadastrar(_c.Admin, "Sem corredor", []));
        var gerente = GerenteExecutivo.Cadastrar(_c.Admin, "Gerência Fictícia", [Cenario.CorredorNorte.Id]);

        gerente.DefinirCorredores(_c.Admin, [Cenario.CorredorSudeste.Id, Cenario.CorredorSudeste.Id]);

        Assert.False(gerente.AtendeCorredor(Cenario.CorredorNorte.Id));
        Assert.True(gerente.AtendeCorredor(Cenario.CorredorSudeste.Id));
        Assert.Single(gerente.Corredores);
        Assert.Throws<RegraNegocioException>(() => gerente.DefinirCorredores(_c.Admin, []));
    }
}
