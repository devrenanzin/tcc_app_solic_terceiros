using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Campos do formulário da demanda (seção 8.1): obrigatórios, condicionais e limites.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class FormularioTests
{
    [Fact]
    public void Formulario_completo_e_aceito_e_normalizado()
    {
        var dados = Cenario.Dados() with
        {
            LocalidadeVaga = "  Vitória  ",
            AreaSolicitante = "   ",
            ResponsavelEfetivoEmail = " Responsavel@UCL.br ",
            PeriodoTemporarioMeses = 6,
            CategoriaCnh = "B",
        };

        var conferidos = dados.Validar();

        Assert.Equal("Vitória", conferidos.LocalidadeVaga);
        Assert.Null(conferidos.AreaSolicitante);
        Assert.Equal("responsavel@ucl.br", conferidos.ResponsavelEfetivoEmail);
        Assert.Null(conferidos.PeriodoTemporarioMeses); // não é temporária
        Assert.Null(conferidos.CategoriaCnh); // não exige CNH
    }

    [Fact]
    public void Formulario_vazio_recusa_com_todos_os_obrigatorios_na_mensagem()
    {
        var erro = Assert.Throws<RegraNegocioException>(() => new DadosSolicitacao().Validar());

        foreach (var campo in new[]
        {
            "Localidade da vaga", "Descrição das atividades", "OS do contrato", "Coletor de custo",
            "Nome do responsável efetivo", "E-mail do responsável efetivo", "Nome do fiscal efetivo",
            "E-mail do fiscal efetivo", "Tipo de demanda", "Gerente executivo", "Corredor", "Modelo de trabalho",
            "Cargo e preço (QQP)", "quantidade de vagas",
        })
        {
            Assert.Contains(campo, erro.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(40_000)]
    public void Quantidade_precisa_ser_maior_que_zero(int quantidade)
        => Assert.Throws<RegraNegocioException>(() => (Cenario.Dados() with { QuantidadeSolicitada = quantidade }).Validar());

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Vaga_temporaria_exige_periodo_em_meses(int? meses)
        => Assert.Throws<RegraNegocioException>(() => (Cenario.Dados() with { Temporaria = true, PeriodoTemporarioMeses = meses }).Validar());

    [Fact]
    public void Vaga_temporaria_guarda_o_periodo()
        => Assert.Equal(6, (Cenario.Dados() with { Temporaria = true, PeriodoTemporarioMeses = 6 }).Validar().PeriodoTemporarioMeses);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("F")]
    public void Cnh_exigida_precisa_de_categoria_da_lista(string? categoria)
        => Assert.Throws<RegraNegocioException>(() => (Cenario.Dados() with { ExigeCnh = true, CategoriaCnh = categoria }).Validar());

    [Theory]
    [InlineData("ab", "AB")]
    [InlineData("E", "E")]
    public void Categoria_da_cnh_aceita_maiusculas_ou_minusculas(string categoria, string gravada)
        => Assert.Equal(gravada, (Cenario.Dados() with { ExigeCnh = true, CategoriaCnh = categoria }).Validar().CategoriaCnh);

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("nome@dominio")]
    [InlineData("nome @ucl.br")]
    public void Email_de_contato_precisa_ser_valido(string email)
        => Assert.Throws<RegraNegocioException>(() => (Cenario.Dados() with { FiscalEfetivoEmail = email }).Validar());

    [Fact]
    public void Email_de_contato_aceita_qualquer_dominio()
        => Assert.Equal("fiscal@empresa.com.br", (Cenario.Dados() with { FiscalEfetivoEmail = "fiscal@empresa.com.br" }).Validar().FiscalEfetivoEmail);

    [Theory]
    [InlineData("AreaSolicitante", 151)]
    [InlineData("ColetorCusto", 31)]
    [InlineData("LocalidadeVaga", 201)]
    public void Textos_respeitam_o_tamanho_do_modelo_fisico(string campo, int tamanho)
    {
        var texto = new string('x', tamanho);
        var dados = campo switch
        {
            "AreaSolicitante" => Cenario.Dados() with { AreaSolicitante = texto },
            "ColetorCusto" => Cenario.Dados() with { ColetorCusto = texto },
            _ => Cenario.Dados() with { LocalidadeVaga = texto },
        };

        Assert.Throws<RegraNegocioException>(() => dados.Validar());
    }
}
