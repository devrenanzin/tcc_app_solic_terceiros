using System.Net;
using System.Text.RegularExpressions;
using Contratacao.Tests.Integracao.Apoio;

namespace Contratacao.Tests.Integracao;

/// <summary>Sair do sistema e entrar com outra conta.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed partial class TelasSessaoTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private static string EmailAdmin => BancoFixture.Admin.Email!.Trim().ToLowerInvariant();

    [Fact]
    public async Task Botao_sair_do_menu_encerra_a_sessao()
    {
        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);

        // O formulário do menu precisa levar o próprio token antifalsificação.
        var inicio = await navegador.HtmlAsync("/");
        var formulario = FormularioSair().Match(inicio);
        Assert.True(formulario.Success, "Formulário Sair sem token no menu.");

        using var saida = await navegador.EnviarDeAsync("/", "/Sair");
        Assert.Equal(HttpStatusCode.Redirect, saida.StatusCode);
        Assert.Equal("/Entrar", saida.Headers.Location?.OriginalString);

        using var depois = await navegador.AbrirAsync("/");
        Assert.StartsWith("http://localhost/Entrar", depois.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Endereco_sair_mostra_a_confirmacao_e_sai()
    {
        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);

        var confirmacao = await navegador.HtmlAsync("/Sair");
        Assert.Contains("Sair agora", confirmacao, StringComparison.Ordinal);

        using var saida = await navegador.EnviarAsync("/Sair");
        Assert.Equal("/Entrar", saida.Headers.Location?.OriginalString);
        Assert.Contains("Você saiu do sistema", await navegador.HtmlAsync("/Entrar"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tela_de_entrar_mostra_quem_esta_conectado_e_permite_trocar_de_conta()
    {
        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);

        var entrar = await navegador.HtmlAsync("/Entrar");

        Assert.Contains("Você já está conectado como", entrar, StringComparison.Ordinal);
        Assert.Contains("Sair e entrar com outra conta", entrar, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sem_login_o_endereco_sair_leva_para_entrar()
    {
        await using var navegador = new Navegador(banco);

        using var resposta = await navegador.AbrirAsync("/Sair");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Entrar", resposta.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [GeneratedRegex("""<form method="post" action="/Sair">\s*<button[^<]*<i[^<]*</i> Sair</button>\s*<input name="__RequestVerificationToken" """)]
    private static partial Regex FormularioSair();
}
