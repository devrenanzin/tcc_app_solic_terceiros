using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>
/// Admin com todos os cadastros e vínculo de conta já cadastrada (Cliente, revisão de 06/10/2026): a conta de
/// Solicitante muda de perfil e continua entrando com o mesmo e-mail e senha.
/// </summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class VinculoContaTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private const string Senha = "senha-segura-1";

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;
    private static string EmailAdmin => BancoFixture.Admin.Email!.Trim().ToLowerInvariant();

    [Fact]
    public async Task Gestor_vincula_conta_de_solicitante_a_equipe_e_ela_entra_como_sesi()
    {
        var (gestorEmail, _) = await GestorAsync(IdsFixos.ContratoNorte);
        await using var pessoa = new Navegador(banco);
        var email = await CadastrarSolicitanteAsync(pessoa);

        await using var gestor = new Navegador(banco);
        await gestor.EntrarPelaPrimeiraVezAsync(gestorEmail, Senha, "senha-propria-2");
        using var vinculo = await gestor.EnviarAsync("/Equipe/Vincular", ("Email", email.ToUpperInvariant()), ("Contrato", IdsFixos.ContratoNorte.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, vinculo.StatusCode);
        Assert.Contains(email, await gestor.HtmlAsync("/Equipe"), StringComparison.Ordinal);

        // A sessão aberta como Solicitante cai (o perfil mudou); a pessoa entra de novo com a mesma senha.
        using var antiga = await pessoa.AbrirAsync("/");
        Assert.StartsWith("http://localhost/Entrar", antiga.Headers.Location?.ToString(), StringComparison.Ordinal);
        await using var deNovo = new Navegador(banco);
        await deNovo.EntrarAsync(email, Senha);
        Assert.Contains("Aguardando aceite", await deNovo.HtmlAsync("/"), StringComparison.Ordinal);

        await using var contexto = banco.NovoContexto();
        var conta = await contexto.Usuarios.SingleAsync(u => u.Email == email, Cancelamento);
        Assert.Equal((Perfil.FuncionarioSesi, IdsFixos.ContratoNorte), (conta.Perfil, conta.ContratoId));
        Assert.True(await contexto.LogsAuditoria.AnyAsync(l => l.EntidadeId == conta.Id && l.Acao == "VinculoContaComoSesi", Cancelamento));
    }

    [Fact]
    public async Task Admin_cadastra_sesi_em_qualquer_equipe_e_vincula_conta_como_gestor()
    {
        var (_, gestorId) = await GestorAsync(IdsFixos.ContratoNorte);
        await using var admin = new Navegador(banco);
        await admin.EntrarAsync(EmailAdmin, BancoFixture.Admin.Senha!);

        var sesiEmail = $"sesi.{Guid.NewGuid():N}@ucl.br";
        using var cadastro = await admin.EnviarAsync("/Admin/FuncionariosSesi/Novo",
            ("Entrada.Nome", "SESI pelo Admin"), ("Entrada.Email", sesiEmail), ("Entrada.SenhaInicial", Senha),
            ("GestorId", gestorId.ToString()), ("Contrato", IdsFixos.ContratoSudeste.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);

        await using var pessoa = new Navegador(banco);
        var email = await CadastrarSolicitanteAsync(pessoa);
        using var vinculo = await admin.EnviarAsync("/Admin/Gestores/Vincular", ("Email", email), ("Contratos", IdsFixos.ContratoSudeste.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, vinculo.StatusCode);

        await using var contexto = banco.NovoContexto();
        var sesi = await contexto.Usuarios.SingleAsync(u => u.Email == sesiEmail, Cancelamento);
        Assert.Equal((gestorId, IdsFixos.ContratoSudeste), (sesi.GestorResponsavelId!.Value, sesi.ContratoId!.Value));
        var novoGestor = await contexto.Usuarios.SingleAsync(u => u.Email == email, Cancelamento);
        Assert.Equal(Perfil.Gestor, novoGestor.Perfil);
        Assert.Equal([IdsFixos.ContratoSudeste], await contexto.GestoresContratos.Where(g => g.GestorId == novoGestor.Id).Select(g => g.ContratoId).ToListAsync(Cancelamento));
        Assert.Contains(sesiEmail, await admin.HtmlAsync("/Admin/FuncionariosSesi"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Conta_com_demanda_em_andamento_ou_inexistente_nao_e_vinculada()
    {
        var admin = await banco.AdminAsync();
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync();
        await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento));

        string emailSolicitante;
        await using (var contexto = banco.NovoContexto())
        {
            emailSolicitante = (await contexto.Usuarios.SingleAsync(u => u.Id == solicitante.Id, Cancelamento)).Email;
        }

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<VincularContaComoSesi, Usuario>(v =>
            v.ExecutarAsync(admin, emailSolicitante, IdsFixos.ContratoNorte, gestor.Id, Cancelamento)));
        Assert.Contains("demandas em andamento", erro.Message, StringComparison.Ordinal);

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<VincularContaComoGestor, Usuario>(v =>
            v.ExecutarAsync(admin, "ninguem@ucl.br", [IdsFixos.ContratoNorte], Cancelamento)));
    }

    private async Task<(string Email, Guid Id)> GestorAsync(Guid contrato)
    {
        var admin = await banco.AdminAsync();
        var email = $"gestor.{Guid.NewGuid():N}@ucl.br";
        var gestor = await banco.ExecutarAsync<CadastrarGestor, Usuario>(c => c.ExecutarAsync(admin, "Gestor de Testes", email, Senha, [contrato], Cancelamento));
        return (email, gestor.Id);
    }

    private static async Task<string> CadastrarSolicitanteAsync(Navegador navegador)
    {
        var email = $"sol.{Guid.NewGuid():N}@ucl.br";
        using var cadastro = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Conta Existente"), ("Entrada.Email", email), ("Entrada.Senha", Senha), ("Entrada.ConfirmacaoSenha", Senha));
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        return email;
    }
}
