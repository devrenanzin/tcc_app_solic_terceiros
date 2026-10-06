using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Usuarios;

/// <summary>Cadastro, hierarquia e situação dos usuários (seções 4.5, 19 e RN11).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class UsuarioTests
{
    private static readonly DateTime Agora = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
    private readonly Cenario _c = new();

    [Fact]
    public void Autocadastro_cria_solicitante_ativo_sem_gestor_com_login_igual_ao_email()
    {
        var usuario = Usuario.CadastrarSolicitante("  Ana Teste ", " Ana.Teste@UCL.BR ", Agora);

        Assert.Equal(Perfil.Solicitante, usuario.Perfil);
        Assert.Equal("Ana Teste", usuario.Nome);
        Assert.Equal("ana.teste@ucl.br", usuario.Email);
        Assert.Equal(usuario.Email, usuario.Login);
        Assert.True(usuario.Ativo);
        Assert.Null(usuario.GestorResponsavelId);
        Assert.Null(usuario.CriadoPorUsuarioId);
        Assert.Equal(Agora, usuario.DataCadastro);
    }

    [Theory]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@ucl.com")]
    [InlineData("ana@ucl.br.com")]
    [InlineData("ana@sub.ucl.br")]
    [InlineData("@ucl.br")]
    [InlineData("ana ucl@ucl.br")]
    [InlineData("")]
    public void Email_fora_do_dominio_ucl_br_e_recusado(string email)
        => Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarSolicitante("Ana", email, Agora));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_e_obrigatorio(string nome)
        => Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarSolicitante(nome, "ana@ucl.br", Agora));

    [Fact]
    public void So_o_admin_cadastra_gestor()
    {
        var gestor = Usuario.CadastrarGestor(_c.Admin, "Bruno Gestor", "bruno@ucl.br", Agora);

        Assert.Equal(Perfil.Gestor, gestor.Perfil);
        Assert.Equal(_c.Admin.Id, gestor.CriadoPorUsuarioId);
        Assert.Null(gestor.GestorResponsavelId);
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarGestor(_c.GestorNorte, "X", "x@ucl.br", Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarGestor(_c.Admin with { Ativo = false }, "X", "x@ucl.br", Agora));
    }

    [Fact]
    public void Gestor_cadastra_funcionario_sesi_na_sua_equipe_e_num_dos_seus_contratos()
    {
        var funcionario = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Carla Sesi", "carla@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);

        Assert.Equal(Perfil.FuncionarioSesi, funcionario.Perfil);
        Assert.Equal(_c.GestorNorte.Id, funcionario.GestorResponsavelId);
        Assert.Equal(_c.GestorNorte.Id, funcionario.CriadoPorUsuarioId);
        Assert.Equal(Cenario.ContratoNorte.Id, funcionario.ContratoId);
    }

    [Fact]
    public void Funcionario_sesi_exige_gestor_ativo_e_contrato()
    {
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.Admin, "X", "x@ucl.br", Cenario.ContratoNorte.Id, _c.Admin, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.GestorInativo, "X", "x@ucl.br", Cenario.ContratoNorte.Id, _c.GestorInativo, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "X", "x@ucl.br", Guid.Empty, _c.GestorNorte, Agora));
    }

    [Fact]
    public void Quem_gerencia_o_cadastro_de_quem()
    {
        var gestor = Usuario.CadastrarGestor(_c.Admin, "Gestor", "gestor@ucl.br", Agora);
        var sesiDoNorte = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        var solicitante = Usuario.CadastrarSolicitante("Sol", "sol@ucl.br", Agora);
        var admin = Usuario.CriarAdminInicial("Admin", "admin@ucl.br", Agora);

        // Admin: Gestores, Solicitantes e qualquer Funcionário da Gerenciadora (Cliente, revisão de 06/10/2026).
        Assert.True(gestor.PodeSerGerenciadoPor(_c.Admin));
        Assert.True(solicitante.PodeSerGerenciadoPor(_c.Admin));
        Assert.True(sesiDoNorte.PodeSerGerenciadoPor(_c.Admin));

        // Gestor: a própria equipe da Gerenciadora e qualquer Solicitante; nunca outro Gestor.
        Assert.True(sesiDoNorte.PodeSerGerenciadoPor(_c.GestorNorte));
        Assert.False(sesiDoNorte.PodeSerGerenciadoPor(_c.GestorSudeste));
        Assert.True(solicitante.PodeSerGerenciadoPor(_c.GestorSudeste));
        Assert.False(gestor.PodeSerGerenciadoPor(_c.GestorNorte));

        // Ninguém gerencia o Admin; Gerenciadora e Solicitante não gerenciam ninguém; inativo não gerencia.
        Assert.False(admin.PodeSerGerenciadoPor(_c.Admin));
        Assert.False(solicitante.PodeSerGerenciadoPor(_c.SesiNorte));
        Assert.False(solicitante.PodeSerGerenciadoPor(_c.OutroSolicitante));
        Assert.False(sesiDoNorte.PodeSerGerenciadoPor(_c.GestorNorte with { Ativo = false }));
    }

    [Fact]
    public void Desativar_e_reativar_sem_excluir()
    {
        var solicitante = Usuario.CadastrarSolicitante("Sol", "sol@ucl.br", Agora);

        solicitante.Desativar(_c.GestorNorte);
        Assert.False(solicitante.Ativo);
        Assert.Throws<RegraNegocioException>(() => solicitante.Desativar(_c.Admin));

        solicitante.Reativar(_c.Admin);
        Assert.True(solicitante.Ativo);
        Assert.Throws<RegraNegocioException>(() => solicitante.Reativar(_c.Admin));
    }

    [Fact]
    public void Gestor_nao_desativa_funcionario_de_outro_gestor()
    {
        var sesiDoNorte = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);

        Assert.Throws<RegraNegocioException>(() => sesiDoNorte.Desativar(_c.GestorSudeste));
        Assert.Throws<RegraNegocioException>(() => sesiDoNorte.AlterarContrato(_c.GestorSudeste, Cenario.ContratoSudeste.Id));
        Assert.True(sesiDoNorte.Ativo);
        Assert.Equal(Cenario.ContratoNorte.Id, sesiDoNorte.ContratoId);
    }

    [Fact]
    public void Gestor_so_coloca_funcionario_num_dos_seus_contratos()
    {
        Assert.Throws<RegraNegocioException>(() =>
            Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoSudeste.Id, _c.GestorNorte, Agora));

        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        Assert.Throws<RegraNegocioException>(() => sesi.AlterarContrato(_c.GestorNorte, Cenario.ContratoSudeste.Id));
        Assert.Equal(Cenario.ContratoNorte.Id, sesi.ContratoId);
    }

    [Fact]
    public void Gestor_com_dois_contratos_troca_o_contrato_do_seu_funcionario()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorDosDois, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorDosDois, Agora);

        sesi.AlterarContrato(_c.GestorDosDois, Cenario.ContratoSudeste.Id);

        Assert.Equal(Cenario.ContratoSudeste.Id, sesi.ContratoId);
    }

    [Fact]
    public void Admin_coloca_funcionario_em_qualquer_contrato_e_desativa()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);

        sesi.AlterarContrato(_c.Admin, Cenario.ContratoSudeste.Id);
        sesi.Desativar(_c.Admin);

        Assert.Equal(Cenario.ContratoSudeste.Id, sesi.ContratoId);
        Assert.False(sesi.Ativo);
    }

    [Fact]
    public void Admin_cadastra_funcionario_sesi_na_equipe_de_qualquer_gestor_e_contrato()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.Admin, "Sesi", "sesi@ucl.br", Cenario.ContratoSudeste.Id, _c.GestorNorte, Agora);

        Assert.Equal((Perfil.FuncionarioSesi, _c.GestorNorte.Id, Cenario.ContratoSudeste.Id), (sesi.Perfil, sesi.GestorResponsavelId, sesi.ContratoId));
        Assert.True(sesi.DeveTrocarSenha);

        // O responsável precisa ser um Gestor ativo; o Gestor só cadastra na própria equipe.
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.Admin, "X", "x@ucl.br", Cenario.ContratoNorte.Id, _c.GestorInativo, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.Admin, "X", "x@ucl.br", Cenario.ContratoNorte.Id, _c.SesiNorte, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.GestorSudeste, "X", "x@ucl.br", Cenario.ContratoSudeste.Id, _c.GestorNorte, Agora));
    }

    [Fact]
    public void Conta_de_solicitante_vira_funcionario_sesi_ou_gestor_com_o_mesmo_login()
    {
        var conta = Usuario.CadastrarSolicitante("Conta Existente", "conta@ucl.br", Agora);
        conta.DefinirSenhaHash("hash-da-senha-propria");

        conta.TornarFuncionarioSesi(_c.GestorNorte, Cenario.ContratoNorte.Id, _c.GestorNorte, demandasEmAndamento: 0);

        Assert.Equal((Perfil.FuncionarioSesi, _c.GestorNorte.Id, Cenario.ContratoNorte.Id), (conta.Perfil, conta.GestorResponsavelId, conta.ContratoId));
        Assert.Equal(("conta@ucl.br", "hash-da-senha-propria", false), (conta.Login, conta.SenhaHash, conta.DeveTrocarSenha));

        var outra = Usuario.CadastrarSolicitante("Outra Conta", "outra@ucl.br", Agora);
        outra.TornarGestor(_c.Admin, demandasEmAndamento: 0);
        Assert.Equal(Perfil.Gestor, outra.Perfil);
    }

    [Fact]
    public void Vinculo_de_conta_segue_as_regras_de_quem_cadastra()
    {
        Usuario Conta() => Usuario.CadastrarSolicitante("Conta", "conta@ucl.br", Agora);

        // Gestor: só na própria equipe e nos seus contratos; o Admin, qualquer um.
        Assert.Throws<RegraNegocioException>(() => Conta().TornarFuncionarioSesi(_c.GestorNorte, Cenario.ContratoSudeste.Id, _c.GestorNorte, 0));
        Assert.Throws<RegraNegocioException>(() => Conta().TornarFuncionarioSesi(_c.GestorNorte, Cenario.ContratoNorte.Id, _c.GestorDosDois, 0));
        Conta().TornarFuncionarioSesi(_c.Admin, Cenario.ContratoSudeste.Id, _c.GestorNorte, 0);

        // Só o Admin torna alguém Gestor; Solicitante e Gerenciadora não vinculam ninguém.
        Assert.Throws<RegraNegocioException>(() => Conta().TornarGestor(_c.GestorNorte, 0));
        Assert.Throws<RegraNegocioException>(() => Conta().TornarFuncionarioSesi(_c.SesiNorte, Cenario.ContratoNorte.Id, _c.GestorNorte, 0));
    }

    [Fact]
    public void So_conta_ativa_de_solicitante_sem_demandas_em_andamento_muda_de_perfil()
    {
        var comDemandas = Usuario.CadastrarSolicitante("Conta", "conta@ucl.br", Agora);
        var erro = Assert.Throws<RegraNegocioException>(() => comDemandas.TornarFuncionarioSesi(_c.Admin, Cenario.ContratoNorte.Id, _c.GestorNorte, 1));
        Assert.Contains("demandas em andamento", erro.Message, StringComparison.Ordinal);

        var desativada = Usuario.CadastrarSolicitante("Conta", "conta@ucl.br", Agora);
        desativada.Desativar(_c.Admin);
        Assert.Throws<RegraNegocioException>(() => desativada.TornarGestor(_c.Admin, 0));

        var jaSesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        Assert.Throws<RegraNegocioException>(() => jaSesi.TornarGestor(_c.Admin, 0));
        Assert.Equal(Perfil.FuncionarioSesi, jaSesi.Perfil);
    }

    [Fact]
    public void Quem_recebe_senha_de_outra_pessoa_troca_no_primeiro_acesso()
    {
        var gestor = Usuario.CadastrarGestor(_c.Admin, "Gestor", "gestor@ucl.br", Agora);
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        var solicitante = Usuario.CadastrarSolicitante("Sol", "sol@ucl.br", Agora);
        var admin = Usuario.CriarAdminInicial("Admin", "admin@ucl.br", Agora);

        Assert.True(gestor.DeveTrocarSenha);
        Assert.True(sesi.DeveTrocarSenha);
        Assert.False(solicitante.DeveTrocarSenha);
        Assert.False(admin.DeveTrocarSenha);

        gestor.TrocarSenha("novo-hash");
        Assert.False(gestor.DeveTrocarSenha);
        Assert.Equal("novo-hash", gestor.SenhaHash);
    }

    [Fact]
    public void Admin_transfere_funcionario_sesi_para_gestor_ativo()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        var destino = Usuario.CadastrarGestor(_c.Admin, "Destino", "destino@ucl.br", Agora);
        var solicitante = Usuario.CadastrarSolicitante("Sol", "sol@ucl.br", Agora);

        Assert.Throws<RegraNegocioException>(() => sesi.TransferirPara(_c.GestorSudeste, destino));
        Assert.Throws<RegraNegocioException>(() => solicitante.TransferirPara(_c.Admin, destino));
        Assert.Throws<RegraNegocioException>(() => sesi.TransferirPara(_c.Admin, solicitante));

        sesi.TransferirPara(_c.Admin, destino);
        Assert.Equal(destino.Id, sesi.GestorResponsavelId);
        Assert.Throws<RegraNegocioException>(() => sesi.TransferirPara(_c.Admin, destino));
    }

    [Fact]
    public void Nao_transfere_para_gestor_desativado()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, _c.GestorNorte, Agora);
        var destino = Usuario.CadastrarGestor(_c.Admin, "Destino", "destino@ucl.br", Agora);
        destino.Desativar(_c.Admin);

        Assert.Throws<RegraNegocioException>(() => sesi.TransferirPara(_c.Admin, destino));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Senha_curta_e_recusada(string senha)
        => Assert.Throws<RegraNegocioException>(() => Senha.Validar(senha));

    [Fact]
    public void Senha_com_oito_caracteres_e_aceita() => Senha.Validar("12345678");
}
