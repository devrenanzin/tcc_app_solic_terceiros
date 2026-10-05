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
    public void Gestor_cadastra_funcionario_sesi_na_sua_equipe_e_num_contrato()
    {
        var funcionario = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Carla Sesi", "carla@ucl.br", Cenario.ContratoNorte.Id, Agora);

        Assert.Equal(Perfil.FuncionarioSesi, funcionario.Perfil);
        Assert.Equal(_c.GestorNorte.Id, funcionario.GestorResponsavelId);
        Assert.Equal(_c.GestorNorte.Id, funcionario.CriadoPorUsuarioId);
        Assert.Equal(Cenario.ContratoNorte.Id, funcionario.ContratoId);
    }

    [Fact]
    public void Funcionario_sesi_exige_gestor_ativo_e_contrato()
    {
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.Admin, "X", "x@ucl.br", Cenario.ContratoNorte.Id, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.GestorInativo, "X", "x@ucl.br", Cenario.ContratoNorte.Id, Agora));
        Assert.Throws<RegraNegocioException>(() => Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "X", "x@ucl.br", Guid.Empty, Agora));
    }

    [Fact]
    public void Quem_gerencia_o_cadastro_de_quem()
    {
        var gestor = Usuario.CadastrarGestor(_c.Admin, "Gestor", "gestor@ucl.br", Agora);
        var sesiDoNorte = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, Agora);
        var solicitante = Usuario.CadastrarSolicitante("Sol", "sol@ucl.br", Agora);
        var admin = Usuario.CriarAdminInicial("Admin", "admin@ucl.br", Agora);

        // Admin: só Gestores e Solicitantes.
        Assert.True(gestor.PodeSerGerenciadoPor(_c.Admin));
        Assert.True(solicitante.PodeSerGerenciadoPor(_c.Admin));
        Assert.False(sesiDoNorte.PodeSerGerenciadoPor(_c.Admin));

        // Gestor: a própria equipe SESI e qualquer Solicitante; nunca outro Gestor.
        Assert.True(sesiDoNorte.PodeSerGerenciadoPor(_c.GestorNorte));
        Assert.False(sesiDoNorte.PodeSerGerenciadoPor(_c.GestorSudeste));
        Assert.True(solicitante.PodeSerGerenciadoPor(_c.GestorSudeste));
        Assert.False(gestor.PodeSerGerenciadoPor(_c.GestorNorte));

        // Ninguém gerencia o Admin; SESI e Solicitante não gerenciam ninguém; inativo não gerencia.
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
        var sesiDoNorte = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, Agora);

        Assert.Throws<RegraNegocioException>(() => sesiDoNorte.Desativar(_c.GestorSudeste));
        Assert.Throws<RegraNegocioException>(() => sesiDoNorte.AlterarContrato(_c.GestorSudeste, Cenario.ContratoSudeste.Id));
        Assert.True(sesiDoNorte.Ativo);
        Assert.Equal(Cenario.ContratoNorte.Id, sesiDoNorte.ContratoId);
    }

    [Fact]
    public void Gestor_troca_o_contrato_do_seu_funcionario()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, Agora);

        sesi.AlterarContrato(_c.GestorNorte, Cenario.ContratoSudeste.Id);

        Assert.Equal(Cenario.ContratoSudeste.Id, sesi.ContratoId);
    }

    [Fact]
    public void Admin_transfere_funcionario_sesi_para_gestor_ativo()
    {
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, Agora);
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
        var sesi = Usuario.CadastrarFuncionarioSesi(_c.GestorNorte, "Sesi", "sesi@ucl.br", Cenario.ContratoNorte.Id, Agora);
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
