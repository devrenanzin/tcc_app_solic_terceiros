using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Telas de acesso e cadastro, pela aplicação web, com autorização por perfil e por vínculo.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class TelasAcessoTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private const string Senha = "senha-segura-1";
    private const string NovaSenha = "senha-propria-2";

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;
    private static string EmailAdmin => BancoFixture.Admin.Email!.Trim().ToLowerInvariant();

    [Theory]
    [InlineData("/")]
    [InlineData("/Admin/Gestores")]
    [InlineData("/Equipe")]
    [InlineData("/Solicitantes")]
    public async Task Sem_login_toda_pagina_leva_para_entrar(string caminho)
    {
        await using var navegador = new Navegador(banco);

        using var resposta = await navegador.AbrirAsync(caminho);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("http://localhost/Entrar", resposta.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Senha_errada_mostra_mensagem_sem_revelar_o_motivo()
    {
        await using var navegador = new Navegador(banco);

        using var resposta = await navegador.EnviarAsync("/Entrar", ("Entrada.Email", EmailAdmin), ("Entrada.Senha", "senha-errada-1"));
        var html = await resposta.Content.ReadAsStringAsync(Cancelamento);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("E-mail ou senha inválidos, ou usuário desativado.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_cadastra_gestor_pela_tela_com_dois_contratos()
    {
        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);
        var email = $"gestor.tela.{Guid.NewGuid():N}@ucl.br";

        using var resposta = await navegador.EnviarAsync("/Admin/Gestores/Novo",
            ("Entrada.Nome", "Gestor Pela Tela"), ("Entrada.Email", email), ("Entrada.SenhaInicial", Senha),
            ("Contratos", IdsFixos.ContratoNorte.ToString()), ("Contratos", IdsFixos.ContratoSudeste.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var lista = await navegador.HtmlAsync("/Admin/Gestores");
        Assert.Contains("Gestor Pela Tela", lista, StringComparison.Ordinal);
        Assert.Contains("5900118506", lista, StringComparison.Ordinal);
        Assert.Contains("5900125082", lista, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocadastro_pela_tela_entra_como_solicitante_sem_acesso_as_telas_de_gestao()
    {
        await using var navegador = new Navegador(banco);
        var email = $"sol.tela.{Guid.NewGuid():N}@ucl.br";

        using var cadastro = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Solicitante Pela Tela"), ("Entrada.Email", email),
            ("Entrada.Senha", Senha), ("Entrada.ConfirmacaoSenha", Senha));

        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        Assert.Contains("Nova demanda", await navegador.HtmlAsync("/"), StringComparison.Ordinal);
        foreach (var caminho in new[] { "/Admin/Gestores", "/Equipe", "/Solicitantes" })
        {
            using var negado = await navegador.AbrirAsync(caminho);
            Assert.StartsWith("http://localhost/AcessoNegado", negado.Headers.Location?.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Autocadastro_com_email_de_outro_dominio_mostra_o_erro()
    {
        await using var navegador = new Navegador(banco);

        using var resposta = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Fora do Domínio"), ("Entrada.Email", "fora@gmail.com"),
            ("Entrada.Senha", Senha), ("Entrada.ConfirmacaoSenha", Senha));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("@ucl.br", await resposta.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gestor_cadastra_funcionario_sesi_pela_tela_e_nao_acessa_a_administracao()
    {
        var email = $"gestor.equipe.{Guid.NewGuid():N}@ucl.br";
        var admin = await banco.AdminAsync();
        await banco.ExecutarAsync<CadastrarGestor, Usuario>(c =>
            c.ExecutarAsync(admin, "Gestor Equipe", email, Senha, [IdsFixos.ContratoNorte], Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarPelaPrimeiraVezAsync(email, Senha, NovaSenha);

        var formulario = await navegador.HtmlAsync("/Equipe/Novo");
        Assert.Contains("5900125082", formulario, StringComparison.Ordinal);
        Assert.DoesNotContain("5900118506", formulario, StringComparison.Ordinal);

        using var resposta = await navegador.EnviarAsync("/Equipe/Novo",
            ("Entrada.Nome", "Funcionário Pela Tela"), ("Entrada.Email", $"sesi.tela.{Guid.NewGuid():N}@ucl.br"),
            ("Entrada.SenhaInicial", Senha), ("Contrato", IdsFixos.ContratoNorte.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var equipe = await navegador.HtmlAsync("/Equipe");
        Assert.Contains("Funcionário Pela Tela", equipe, StringComparison.Ordinal);
        Assert.Contains("5900125082", equipe, StringComparison.Ordinal);

        using var negado = await navegador.AbrirAsync("/Admin/Gestores");
        Assert.StartsWith("http://localhost/AcessoNegado", negado.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_primeiro_acesso_o_gestor_so_chega_a_troca_de_senha()
    {
        var email = $"gestor.primeiro.{Guid.NewGuid():N}@ucl.br";
        var admin = await banco.AdminAsync();
        await banco.ExecutarAsync<CadastrarGestor, Usuario>(c =>
            c.ExecutarAsync(admin, "Gestor Primeiro Acesso", email, Senha, [IdsFixos.ContratoNorte], Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(email, Senha);

        foreach (var caminho in new[] { "/", "/Equipe", "/Solicitantes" })
        {
            using var desviada = await navegador.AbrirAsync(caminho);
            Assert.Equal(HttpStatusCode.Redirect, desviada.StatusCode);
            Assert.Equal("/TrocarSenha", desviada.Headers.Location?.OriginalString);
        }

        Assert.Contains("Este é seu primeiro acesso.", await navegador.HtmlAsync("/TrocarSenha"), StringComparison.Ordinal);
        using var troca = await navegador.EnviarAsync("/TrocarSenha",
            ("Entrada.SenhaAtual", Senha), ("Entrada.NovaSenha", NovaSenha), ("Entrada.ConfirmacaoSenha", NovaSenha));
        Assert.Equal(HttpStatusCode.Redirect, troca.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await navegador.AbrirAsync("/Equipe")).StatusCode);

        await using var outroNavegador = new Navegador(banco);
        await outroNavegador.EntrarAsync(email, NovaSenha);
        Assert.Equal(HttpStatusCode.OK, (await outroNavegador.AbrirAsync("/Equipe")).StatusCode);
    }

    [Fact]
    public async Task Admin_troca_o_contrato_de_funcionario_pela_tela()
    {
        var admin = await banco.AdminAsync();
        var gestor = await banco.ExecutarAsync<CadastrarGestor, Usuario>(c =>
            c.ExecutarAsync(admin, "Gestor Norte", $"gestor.norte.{Guid.NewGuid():N}@ucl.br", Senha, [IdsFixos.ContratoNorte], Cancelamento));
        var sesi = await banco.ExecutarAsync<CadastrarFuncionarioSesi, Usuario>(async c =>
            await c.ExecutarAsync(await banco.AtorAsync(gestor.Id), "Sesi Para Trocar", $"sesi.troca.{Guid.NewGuid():N}@ucl.br", Senha, IdsFixos.ContratoNorte, Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);
        Assert.Contains("Sesi Para Trocar", await navegador.HtmlAsync("/Admin/FuncionariosSesi"), StringComparison.Ordinal);

        using var resposta = await navegador.EnviarAsync($"/Admin/FuncionariosSesi/Contrato?id={sesi.Id}",
            ("Id", sesi.Id.ToString()), ("Contrato", IdsFixos.ContratoSudeste.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        await using var contexto = banco.NovoContexto();
        Assert.Equal(IdsFixos.ContratoSudeste, (await contexto.Usuarios.SingleAsync(u => u.Id == sesi.Id, Cancelamento)).ContratoId);
    }

    [Fact]
    public async Task Gestor_nao_abre_o_contrato_de_funcionario_de_outro_gestor()
    {
        var admin = await banco.AdminAsync();
        var emailA = $"gestor.a.{Guid.NewGuid():N}@ucl.br";
        var gestorA = await banco.ExecutarAsync<CadastrarGestor, Usuario>(c =>
            c.ExecutarAsync(admin, "Gestor A", emailA, Senha, [IdsFixos.ContratoNorte], Cancelamento));
        var gestorB = await banco.ExecutarAsync<CadastrarGestor, Usuario>(c =>
            c.ExecutarAsync(admin, "Gestor B", $"gestor.b.{Guid.NewGuid():N}@ucl.br", Senha, [IdsFixos.ContratoNorte], Cancelamento));
        var sesiDeB = await banco.ExecutarAsync<CadastrarFuncionarioSesi, Usuario>(async c =>
            await c.ExecutarAsync(await banco.AtorAsync(gestorB.Id), "Sesi de B", $"sesi.b.{Guid.NewGuid():N}@ucl.br", Senha, IdsFixos.ContratoNorte, Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarPelaPrimeiraVezAsync(emailA, Senha, NovaSenha);
        using var resposta = await navegador.AbrirAsync($"/Equipe/Contrato?id={sesiDeB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.DoesNotContain("Sesi de B", await navegador.HtmlAsync("/Equipe"), StringComparison.Ordinal);
        Assert.NotEqual(gestorA.Id, gestorB.Id);
    }

    [Fact]
    public async Task Usuario_desativado_perde_a_sessao_na_proxima_pagina()
    {
        var email = $"sol.sessao.{Guid.NewGuid():N}@ucl.br";
        var solicitante = await banco.ExecutarAsync<CadastrarSolicitante, Usuario>(c =>
            c.ExecutarAsync("Solicitante Sessão", email, Senha, null, Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(email, Senha);
        Assert.Equal(HttpStatusCode.OK, (await navegador.AbrirAsync("/")).StatusCode);

        var admin = await banco.AdminAsync();
        await banco.ExecutarAsync<AlterarSituacaoUsuario>(a => a.ExecutarAsync(admin, solicitante.Id, false, Cancelamento));

        using var depois = await navegador.AbrirAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, depois.StatusCode);
        Assert.StartsWith("http://localhost/Entrar", depois.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_desativa_solicitante_pela_tela_e_a_acao_fica_na_auditoria_com_o_ip()
    {
        var solicitante = await banco.ExecutarAsync<CadastrarSolicitante, Usuario>(c =>
            c.ExecutarAsync("Solicitante Lista", $"sol.lista.{Guid.NewGuid():N}@ucl.br", Senha, null, Cancelamento));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);
        using var resposta = await navegador.EnviarAsync("/Solicitantes?handler=Situacao",
            ("id", solicitante.Id.ToString()), ("ativo", "false"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var pagina = await navegador.HtmlAsync("/Solicitantes");
        Assert.Contains("Solicitante desativado.", pagina, StringComparison.Ordinal);
        await using var contexto = banco.NovoContexto();
        Assert.False((await contexto.Usuarios.SingleAsync(u => u.Id == solicitante.Id, Cancelamento)).Ativo);
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == solicitante.Id && l.Acao == "Desativacao", Cancelamento);
        Assert.Equal("Admin", log.PerfilUsuario);
        Assert.Equal(Navegador.Ip, log.EnderecoIp);
    }
}
