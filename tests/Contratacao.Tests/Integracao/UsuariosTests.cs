using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Casos de uso de acesso e cadastro (UC01, UC12–14, UC19) com o banco real e a auditoria.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class UsuariosTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private const string SenhaPadrao = "senha-segura-1";

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Autocadastro_cria_solicitante_com_hash_e_auditoria_e_permite_entrar()
    {
        var email = Email("ana");

        var criado = await banco.ExecutarAsync<CadastrarSolicitante, Usuario>(c =>
            c.ExecutarAsync("Ana Teste", email.ToUpperInvariant(), SenhaPadrao, "10.1.1.1", Cancelamento));

        await using var contexto = banco.NovoContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == criado.Id, Cancelamento);
        Assert.Equal(Perfil.Solicitante, usuario.Perfil);
        Assert.Equal(email, usuario.Login);
        Assert.DoesNotContain(SenhaPadrao, usuario.SenhaHash);

        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == usuario.Id, Cancelamento);
        Assert.Equal(("AutocadastroSolicitante", "Solicitante", "10.1.1.1"), (log.Acao, log.PerfilUsuario, log.EnderecoIp));

        var autenticado = await banco.ExecutarAsync<AutenticarUsuario, Usuario?>(a => a.ExecutarAsync(email, SenhaPadrao, Cancelamento));
        Assert.Equal(usuario.Id, autenticado?.Id);
    }

    [Fact]
    public async Task Autocadastro_recusa_email_repetido_senha_curta_e_outro_dominio()
    {
        var email = Email("bia");
        await CadastrarSolicitanteAsync(email);

        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarSolicitanteAsync(email));
        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarSolicitanteAsync(Email("curta"), "1234567"));
        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarSolicitanteAsync("bia@gmail.com"));
    }

    [Fact]
    public async Task Autenticacao_recusa_senha_errada_email_de_outro_dominio_e_usuario_desativado()
    {
        var email = Email("caio");
        var solicitante = await CadastrarSolicitanteAsync(email);

        Assert.Null(await AutenticarAsync(email, "senha-errada-1"));
        Assert.Null(await AutenticarAsync("caio@gmail.com", SenhaPadrao));
        Assert.Null(await AutenticarAsync(Email("ninguem"), SenhaPadrao));

        var admin = await banco.AdminAsync();
        await banco.ExecutarAsync<AlterarSituacaoUsuario>(a => a.ExecutarAsync(admin, solicitante.Id, false, Cancelamento));
        Assert.Null(await AutenticarAsync(email, SenhaPadrao));
    }

    [Fact]
    public async Task Autenticacao_registra_o_ultimo_acesso()
    {
        var email = Email("dani");
        await CadastrarSolicitanteAsync(email);
        banco.Relogio.Avancar(TimeSpan.FromMinutes(5));

        var usuario = await AutenticarAsync(email, SenhaPadrao);

        await using var contexto = banco.NovoContexto();
        var gravado = await contexto.Usuarios.SingleAsync(u => u.Id == usuario!.Id, Cancelamento);
        Assert.Equal(banco.Relogio.AgoraUtc, gravado.DataUltimoAcesso);
    }

    [Fact]
    public async Task Admin_cadastra_gestor_com_contratos_e_o_ator_do_gestor_traz_os_contratos()
    {
        var admin = await banco.AdminAsync();

        var gestor = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte, IdsFixos.ContratoSudeste);

        var ator = await banco.AtorAsync(gestor.Id);
        Assert.Equal(Perfil.Gestor, ator.Perfil);
        Assert.True(ator.Contratos.SetEquals([IdsFixos.ContratoNorte, IdsFixos.ContratoSudeste]));
        Assert.Equal("10.0.0.9", ator.EnderecoIp);

        await using var contexto = banco.NovoContexto();
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == gestor.Id && l.Acao == "CadastroGestor", Cancelamento);
        Assert.Equal(admin.Id, log.UsuarioId);
        Assert.Contains("5900118506, 5900125082", log.NovoValor, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gestor_nao_cadastra_gestor_e_gestor_precisa_de_contrato()
    {
        var admin = await banco.AdminAsync();
        var gestor = await banco.AtorAsync((await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte)).Id);

        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarGestorAsync(gestor, IdsFixos.ContratoNorte));
        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarGestorAsync(admin));
        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarGestorAsync(admin, Guid.NewGuid()));
    }

    [Fact]
    public async Task Gestor_cadastra_funcionario_sesi_so_nos_seus_contratos_e_o_admin_troca_para_outro()
    {
        var admin = await banco.AdminAsync();
        var gestor = await banco.AtorAsync((await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte)).Id);

        await Assert.ThrowsAsync<RegraNegocioException>(() => CadastrarSesiAsync(gestor, IdsFixos.ContratoSudeste));
        var funcionario = await CadastrarSesiAsync(gestor, IdsFixos.ContratoNorte);
        Assert.True((await banco.AtorAsync(funcionario.Id)).Contratos.SetEquals([IdsFixos.ContratoNorte]));

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<AlterarContratoFuncionarioSesi>(a =>
            a.ExecutarAsync(gestor, funcionario.Id, IdsFixos.ContratoSudeste, Cancelamento)));
        await banco.ExecutarAsync<AlterarContratoFuncionarioSesi>(a =>
            a.ExecutarAsync(admin, funcionario.Id, IdsFixos.ContratoSudeste, Cancelamento));

        var ator = await banco.AtorAsync(funcionario.Id);
        Assert.Equal(Perfil.FuncionarioSesi, ator.Perfil);
        Assert.True(ator.Contratos.SetEquals([IdsFixos.ContratoSudeste]));

        var equipe = await banco.ExecutarAsync<IUsuarios, IReadOnlyList<ResumoUsuario>>(u =>
            u.ListarAsync(Perfil.FuncionarioSesi, gestor.Id, Cancelamento));
        var resumo = Assert.Single(equipe);
        Assert.Equal(["5900118506"], resumo.Contratos);

        await using var contexto = banco.NovoContexto();
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == funcionario.Id && l.Acao == "AlteracaoContratoSesi", Cancelamento);
        Assert.Equal(("5900125082", "5900118506", "Admin"), (log.ValorAnterior, log.NovoValor, log.PerfilUsuario));
    }

    [Fact]
    public async Task Troca_de_senha_confere_a_atual_libera_o_acesso_e_fica_na_auditoria()
    {
        var admin = await banco.AdminAsync();
        var email = Email("troca");
        var gestor = await CadastrarGestorAsync(admin, email, IdsFixos.ContratoNorte);
        Assert.True(gestor.DeveTrocarSenha);
        var ator = await banco.AtorAsync(gestor.Id);

        await Assert.ThrowsAsync<RegraNegocioException>(() => TrocarSenhaAsync(ator, "senha-errada-1", "nova-senha-123"));
        await Assert.ThrowsAsync<RegraNegocioException>(() => TrocarSenhaAsync(ator, SenhaPadrao, SenhaPadrao));
        await Assert.ThrowsAsync<RegraNegocioException>(() => TrocarSenhaAsync(ator, SenhaPadrao, "curta"));

        var trocado = await TrocarSenhaAsync(ator, SenhaPadrao, "nova-senha-123");

        Assert.False(trocado.DeveTrocarSenha);
        Assert.Null(await AutenticarAsync(email, SenhaPadrao));
        Assert.Equal(gestor.Id, (await AutenticarAsync(email, "nova-senha-123"))?.Id);
        await using var contexto = banco.NovoContexto();
        Assert.True(await contexto.LogsAuditoria.AnyAsync(l => l.EntidadeId == gestor.Id && l.Acao == "TrocaSenha", Cancelamento));
    }

    private Task<Usuario> TrocarSenhaAsync(Ator ator, string atual, string nova)
        => banco.ExecutarAsync<TrocarSenha, Usuario>(t => t.ExecutarAsync(ator, atual, nova, Cancelamento));

    [Fact]
    public async Task Gestor_nao_gerencia_equipe_de_outro_gestor_mas_gerencia_qualquer_solicitante()
    {
        var admin = await banco.AdminAsync();
        var gestorA = await banco.AtorAsync((await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte)).Id);
        var gestorB = await banco.AtorAsync((await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte)).Id);
        var sesiDeA = await CadastrarSesiAsync(gestorA, IdsFixos.ContratoNorte);
        var solicitante = await CadastrarSolicitanteAsync(Email("eva"));

        await Assert.ThrowsAsync<RegraNegocioException>(() => AlterarSituacaoAsync(gestorB, sesiDeA.Id, false));
        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<AlterarContratoFuncionarioSesi>(a =>
            a.ExecutarAsync(gestorB, sesiDeA.Id, IdsFixos.ContratoSudeste, Cancelamento)));
        await AlterarSituacaoAsync(admin, sesiDeA.Id, false); // o Admin gerencia qualquer Gerenciadora (Cliente)

        await AlterarSituacaoAsync(gestorB, solicitante.Id, false);
        await AlterarSituacaoAsync(gestorA, solicitante.Id, true);

        await using var contexto = banco.NovoContexto();
        Assert.False((await contexto.Usuarios.SingleAsync(u => u.Id == sesiDeA.Id, Cancelamento)).Ativo);
        Assert.Equal(2, await contexto.LogsAuditoria.CountAsync(
            l => l.EntidadeId == solicitante.Id && (l.Acao == "Desativacao" || l.Acao == "Reativacao"), Cancelamento));
    }

    [Fact]
    public async Task Gestor_desativado_nao_entra_e_a_equipe_continua_ativa()
    {
        var admin = await banco.AdminAsync();
        var email = Email("gestor");
        var gestor = await CadastrarGestorAsync(admin, email, IdsFixos.ContratoNorte);
        var sesi = await CadastrarSesiAsync(await banco.AtorAsync(gestor.Id), IdsFixos.ContratoNorte);

        await AlterarSituacaoAsync(admin, gestor.Id, false);

        Assert.Null(await AutenticarAsync(email, SenhaPadrao));
        await Assert.ThrowsAsync<InvalidOperationException>(() => banco.AtorAsync(gestor.Id));
        Assert.Equal(Perfil.FuncionarioSesi, (await banco.AtorAsync(sesi.Id)).Perfil);
    }

    [Fact]
    public async Task Admin_redefine_os_contratos_do_gestor_com_auditoria()
    {
        var admin = await banco.AdminAsync();
        var gestor = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte);

        await banco.ExecutarAsync<DefinirContratosGestor>(d => d.ExecutarAsync(admin, gestor.Id, [IdsFixos.ContratoSudeste], Cancelamento));

        Assert.True((await banco.AtorAsync(gestor.Id)).Contratos.SetEquals([IdsFixos.ContratoSudeste]));
        await using var contexto = banco.NovoContexto();
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == gestor.Id && l.Acao == "AlteracaoContratosGestor", Cancelamento);
        Assert.Equal(("5900125082", "5900118506"), (log.ValorAnterior, log.NovoValor));

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<DefinirContratosGestor>(d =>
            d.ExecutarAsync(admin, gestor.Id, [], Cancelamento)));
    }

    [Fact]
    public async Task Admin_transfere_vinculo_com_justificativa_e_auditoria()
    {
        var admin = await banco.AdminAsync();
        var origem = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte);
        var destino = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte);
        var sesi = await CadastrarSesiAsync(await banco.AtorAsync(origem.Id), IdsFixos.ContratoNorte);
        await AlterarSituacaoAsync(admin, origem.Id, false);

        await Assert.ThrowsAsync<RegraNegocioException>(() => TransferirAsync(admin, sesi.Id, destino.Id, "  "));
        await TransferirAsync(admin, sesi.Id, destino.Id, "Gestor de origem desligado.");

        await using var contexto = banco.NovoContexto();
        Assert.Equal(destino.Id, (await contexto.Usuarios.SingleAsync(u => u.Id == sesi.Id, Cancelamento)).GestorResponsavelId);
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == sesi.Id && l.Acao == "TransferenciaVinculo", Cancelamento);
        Assert.Equal((origem.Id.ToString(), destino.Id.ToString()), (log.ValorAnterior, log.NovoValor));
        Assert.Equal("Gestor de origem desligado.", log.Justificativa);
        Assert.Equal("Admin", log.PerfilUsuario);
    }

    [Fact]
    public async Task So_o_admin_transfere_e_so_para_gestor_ativo()
    {
        var admin = await banco.AdminAsync();
        var origem = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte);
        var inativo = await CadastrarGestorAsync(admin, IdsFixos.ContratoNorte);
        await AlterarSituacaoAsync(admin, inativo.Id, false);
        var gestorOrigem = await banco.AtorAsync(origem.Id);
        var sesi = await CadastrarSesiAsync(gestorOrigem, IdsFixos.ContratoNorte);

        await Assert.ThrowsAsync<RegraNegocioException>(() => TransferirAsync(gestorOrigem, sesi.Id, origem.Id, "Motivo."));
        await Assert.ThrowsAsync<RegraNegocioException>(() => TransferirAsync(admin, sesi.Id, inativo.Id, "Motivo."));
    }

    private static string Email(string prefixo) => $"{prefixo}.{Guid.NewGuid():N}@ucl.br";

    private Task<Usuario> CadastrarSolicitanteAsync(string email, string senha = SenhaPadrao)
        => banco.ExecutarAsync<CadastrarSolicitante, Usuario>(c => c.ExecutarAsync("Solicitante Teste", email, senha, "10.1.1.1", Cancelamento));

    private Task<Usuario?> AutenticarAsync(string email, string senha)
        => banco.ExecutarAsync<AutenticarUsuario, Usuario?>(a => a.ExecutarAsync(email, senha, Cancelamento));

    private Task<Usuario> CadastrarGestorAsync(Ator admin, params Guid[] contratos)
        => CadastrarGestorAsync(admin, Email("gestor"), contratos);

    private Task<Usuario> CadastrarGestorAsync(Ator admin, string email, params Guid[] contratos)
        => banco.ExecutarAsync<CadastrarGestor, Usuario>(c => c.ExecutarAsync(admin, "Gestor Teste", email, SenhaPadrao, contratos, Cancelamento));

    private Task<Usuario> CadastrarSesiAsync(Ator gestor, Guid contrato)
        => banco.ExecutarAsync<CadastrarFuncionarioSesi, Usuario>(c =>
            c.ExecutarAsync(gestor, "Funcionário Teste", Email("sesi"), SenhaPadrao, contrato, Cancelamento));

    private Task AlterarSituacaoAsync(Ator ator, Guid usuarioId, bool ativo)
        => banco.ExecutarAsync<AlterarSituacaoUsuario>(a => a.ExecutarAsync(ator, usuarioId, ativo, Cancelamento));

    private Task TransferirAsync(Ator ator, Guid funcionarioId, Guid destinoId, string justificativa)
        => banco.ExecutarAsync<TransferirVinculo>(t => t.ExecutarAsync(ator, [funcionarioId], destinoId, justificativa, Cancelamento));
}
