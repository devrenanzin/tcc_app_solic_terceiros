using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Catalogos;

/// <summary>OS de cada contrato (Cliente, revisão de 06/10/2026): mantidas só pelo Admin.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class OrdemServicoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Admin_cadastra_a_os_no_contrato_e_desativa()
    {
        var os = OrdemServico.Cadastrar(_c.Admin, Cenario.ContratoNorte.Id, " 15 ");

        Assert.Equal(("15", Cenario.ContratoNorte.Id, true), (os.Numero, os.ContratoId, os.Ativo));

        os.AlterarSituacao(_c.Admin, false);
        Assert.False(os.Ativo);
        Assert.Throws<RegraNegocioException>(() => os.AlterarSituacao(_c.Admin, false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    public void Numero_e_obrigatorio_e_tem_ate_5_caracteres(string numero)
        => Assert.Throws<RegraNegocioException>(() => OrdemServico.Cadastrar(_c.Admin, Cenario.ContratoNorte.Id, numero));

    [Fact]
    public void Contrato_e_obrigatorio()
        => Assert.Throws<RegraNegocioException>(() => OrdemServico.Cadastrar(_c.Admin, Guid.Empty, "15"));

    [Theory]
    [InlineData("Gestor")]
    [InlineData("Solicitante")]
    [InlineData("FuncionarioSesi")]
    public void So_o_admin_mantem_as_os(string perfil)
    {
        var ator = Cenario.Novo(Enum.Parse<Perfil>(perfil), Cenario.ContratoNorte);

        Assert.Throws<RegraNegocioException>(() => OrdemServico.Cadastrar(ator, Cenario.ContratoNorte.Id, "15"));
        Assert.Throws<RegraNegocioException>(() => Cenario.OsNorte.AlterarSituacao(ator, false));
    }
}
