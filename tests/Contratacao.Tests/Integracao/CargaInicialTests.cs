using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Parametros;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Carga;
using Contratacao.Web.Infrastructure.Persistencia;
using Contratacao.Web.Infrastructure.Persistencia.Catalogos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Migration inicial e dados iniciais (seção 23), num SQL Server real.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class CargaInicialTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migration_inicial_aplicada_e_o_modelo_nao_tem_mudancas_pendentes()
    {
        await using var contexto = banco.NovoContexto();

        var aplicadas = await contexto.Database.GetAppliedMigrationsAsync(Cancelamento);

        Assert.Contains(aplicadas, m => m.EndsWith("_Inicial", StringComparison.Ordinal));
        Assert.False(contexto.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Perfis_etapas_status_e_prazo_do_sla()
    {
        await using var contexto = banco.NovoContexto();

        Assert.Equal(
            ["Admin", "FuncionarioSesi", "Gestor", "Solicitante"],
            await contexto.Set<LinhaPerfil>().Select(p => p.Nome).OrderBy(n => n).ToListAsync(Cancelamento));
        Assert.Equal(
            ["Solicitação", "Validação do Gestor", "Validação SESI", "Recrutamento", "Entrevistas", "Exames Médicos", "Contratação"],
            await contexto.Set<LinhaEtapa>().OrderBy(e => e.Ordem).Select(e => e.Nome).ToListAsync(Cancelamento));
        Assert.Equal(6, await contexto.Set<LinhaStatus>().CountAsync(Cancelamento));

        var prazo = await contexto.Parametros.SingleAsync(p => p.Chave == ParametroSistema.PrazoSlaDias, Cancelamento);
        Assert.Equal(45, prazo.ValorInteiro());
    }

    [Fact]
    public async Task Catalogos_do_formulario()
    {
        await using var contexto = banco.NovoContexto();

        Assert.Equal(["Híbrido", "Presencial", "Remoto"],
            await contexto.ModelosTrabalho.Select(m => m.Nome).OrderBy(n => n).ToListAsync(Cancelamento));
        Assert.Equal("Nova contratação", (await contexto.TiposDemanda.SingleAsync(Cancelamento)).Nome);
        // A carga não cria gerentes (são nomes de pessoas); o único é o fictício inserido pelo BancoFixture.
        Assert.Equal([BancoFixture.GerenteExecutivoId], await contexto.GerentesExecutivos.Select(g => g.Id).ToListAsync(Cancelamento));

        var equipamentos = await contexto.ItensEquipamento.ToDictionaryAsync(i => i.Nome, i => i.Valor, Cancelamento);
        Assert.Equal(3, equipamentos.Count);
        Assert.Equal(444.35m, equipamentos["Notebook"]);
        Assert.Equal(53.93m, equipamentos["Segunda tela"]);
        Assert.Equal(118.64m, equipamentos["Celular"]);
    }

    [Fact]
    public async Task Veiculo_e_rastreador_ficaram_fora_do_mvp()
    {
        await using var contexto = banco.NovoContexto();

        var tabelaVeiculo = await contexto.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Veiculo'")
            .SingleAsync(Cancelamento);

        Assert.Equal(0, tabelaVeiculo);
        Assert.False(await contexto.ItensEquipamento.AnyAsync(i => i.Nome == "Rastreador", Cancelamento));
    }

    [Fact]
    public async Task Contratada_sesi_sem_cnpj_e_os_dois_contratos()
    {
        await using var contexto = banco.NovoContexto();

        var contratada = await contexto.Contratadas.SingleAsync(Cancelamento);
        Assert.Equal("SESI", contratada.RazaoSocial);
        Assert.Null(contratada.Cnpj);

        var contratos = await contexto.Contratos.OrderBy(c => c.Numero).ToListAsync(Cancelamento);
        Assert.Equal(["5900118506", "5900125082"], contratos.Select(c => c.Numero));
        Assert.All(contratos, c => Assert.Equal(contratada.Id, c.ContratadaId));
    }

    [Fact]
    public async Task Corredores_caem_no_contrato_da_sua_regiao()
    {
        await using var contexto = banco.NovoContexto();

        var corredores = await (
            from corredor in contexto.Corredores
            join regiao in contexto.QqpRegioes on corredor.RegiaoId equals regiao.Id
            join contrato in contexto.Contratos on corredor.ContratoId equals contrato.Id
            orderby regiao.Nome, corredor.Nome
            select corredor.Nome + " / " + regiao.Nome + " / " + contrato.Numero).ToListAsync(Cancelamento);

        Assert.Equal(
            [
                "C. Integrado / QQP NORTE / 5900125082",
                "Norte / QQP NORTE / 5900125082",
                "Pelotização / QQP NORTE / 5900125082",
                "C. Integrado / QQP SUDESTE / 5900118506",
                "Pelotização / QQP SUDESTE / 5900118506",
                "Sudeste / QQP SUDESTE / 5900118506",
                "Sul / QQP SUDESTE / 5900118506",
            ],
            corredores);
    }

    [Fact]
    public async Task Catalogo_qqp_importado_do_csv()
    {
        await using var contexto = banco.NovoContexto();

        Assert.Equal(1019, await contexto.ItensQqp.CountAsync(Cancelamento));
        Assert.Equal(2, await contexto.QqpRegioes.CountAsync(Cancelamento));
        Assert.Equal(18, await contexto.QqpFuncoes.CountAsync(Cancelamento));
        Assert.Equal(5, await contexto.QqpClassificacoes.CountAsync(Cancelamento));
        Assert.Equal(8, await contexto.QqpNiveis.CountAsync(Cancelamento));
        Assert.Equal(6, await contexto.QqpCargasHorarias.CountAsync(Cancelamento));
        Assert.Equal(Enumerable.Range(1, 8),
            (await contexto.QqpNiveis.OrderBy(n => n.Ordem).Select(n => n.Ordem).ToListAsync(Cancelamento)).Select(o => (int)o));

        // Código 1: QQP SUDESTE, Médico, Especialista, I, 44 h, R$ 12.345,67 e R$ 12.345,67.
        var item = await (
            from i in contexto.ItensQqp
            join regiao in contexto.QqpRegioes on i.RegiaoId equals regiao.Id
            join funcao in contexto.QqpFuncoes on i.FuncaoId equals funcao.Id
            join classificacao in contexto.QqpClassificacoes on i.ClassificacaoId equals classificacao.Id
            join nivel in contexto.QqpNiveis on i.NivelId equals nivel.Id
            join carga in contexto.QqpCargasHorarias on i.CargaHorariaId equals carga.Id
            where i.Codigo == 1
            select new { Regiao = regiao.Nome, Funcao = funcao.Nome, Classificacao = classificacao.Nome, Nivel = nivel.Nome, carga.HorasSemanais, i.PisoSalarial, i.PrecoUnitario })
            .SingleAsync(Cancelamento);

        Assert.Equal(("QQP SUDESTE", "Médico", "Especialista", "I", (short)44), (item.Regiao, item.Funcao, item.Classificacao, item.Nivel, item.HorasSemanais));
        Assert.Equal(12345.67m, item.PisoSalarial);
        Assert.Equal(12345.67m, item.PrecoUnitario);
    }

    [Fact]
    public async Task Classificacao_traco_vira_sem_classificacao_e_466_e_467_sao_importados()
    {
        await using var contexto = banco.NovoContexto();
        var linhasComTraco = LeitorCsv.Ler(Path.Combine(PreparacaoBanco.PastaDados, CargaInicial.ArquivoQqp), 8)
            .Count(campos => campos[3] == "-");

        Assert.True(linhasComTraco > 0);
        Assert.Equal(linhasComTraco, await contexto.ItensQqp.CountAsync(i => i.ClassificacaoId == null, Cancelamento));
        Assert.DoesNotContain("-", await contexto.QqpClassificacoes.Select(c => c.Nome).ToListAsync(Cancelamento));

        var repetidos = await contexto.ItensQqp.Where(i => i.Codigo == 466 || i.Codigo == 467).ToListAsync(Cancelamento);
        Assert.Equal(2, repetidos.Count);
        Assert.Equal(repetidos[0].NivelId, repetidos[1].NivelId);
        Assert.NotEqual(repetidos[0].PrecoUnitario, repetidos[1].PrecoUnitario);
    }

    [Fact]
    public async Task Racs_importadas_do_csv_sem_espacos_no_inicio()
    {
        await using var contexto = banco.NovoContexto();

        var racs = await contexto.Racs.OrderBy(r => r.Codigo).ToListAsync(Cancelamento);

        Assert.Equal(13, racs.Count);
        Assert.Equal(("RAC 01", "Trabalhos em altura"), (racs[0].Codigo, racs[0].Nome));
        Assert.Equal(("RAC 13", "Aberturas de Linha e Equipamentos"), (racs[12].Codigo, racs[12].Nome));
        Assert.All(racs, r => Assert.Equal(r.Nome.Trim(), r.Nome));
    }

    [Fact]
    public async Task Admin_inicial_criado_da_configuracao_com_senha_so_em_hash_e_auditado()
    {
        await using var contexto = banco.NovoContexto();

        var admin = await contexto.Usuarios.SingleAsync(u => u.Perfil == Perfil.Admin, Cancelamento);

        Assert.Equal("admin.testes@ucl.br", admin.Email);
        Assert.Equal("admin.testes@ucl.br", admin.Login);
        Assert.True(admin.Ativo);
        Assert.Null(admin.GestorResponsavelId);
        Assert.DoesNotContain(BancoFixture.Admin.Senha!, admin.SenhaHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<Usuario>().VerifyHashedPassword(admin, admin.SenhaHash, BancoFixture.Admin.Senha!));

        var log = await contexto.LogsAuditoria.SingleAsync(l => l.Acao == "AdminInicialCriado", Cancelamento);
        Assert.Equal(admin.Id, log.EntidadeId);
        Assert.Equal("Sistema", log.PerfilUsuario);
        Assert.Null(log.UsuarioId);
    }

    [Fact]
    public async Task Carga_pode_rodar_de_novo_sem_duplicar_nada()
    {
        await using (var contexto = banco.NovoContexto())
        {
            await banco.NovaCarga(contexto).ExecutarAsync(Cancelamento);
        }

        await using var verificacao = banco.NovoContexto();
        Assert.Equal(1019, await verificacao.ItensQqp.CountAsync(Cancelamento));
        Assert.Equal(18, await verificacao.QqpFuncoes.CountAsync(Cancelamento));
        Assert.Equal(13, await verificacao.Racs.CountAsync(Cancelamento));
        Assert.Equal(1, await verificacao.Usuarios.CountAsync(u => u.Perfil == Perfil.Admin, Cancelamento));
    }

    [Fact]
    public async Task Ids_fixos_de_etapa_e_status_sao_gravados_como_uniqueidentifier()
    {
        await using var contexto = banco.NovoContexto();

        var idValidacaoSesi = await contexto.Database
            .SqlQuery<Guid>($"SELECT Id AS Value FROM Etapa WHERE Nome = {"Validação SESI"}")
            .SingleAsync(Cancelamento);

        Assert.Equal(IdsFixos.Etapas[Etapa.ValidacaoSesi], idValidacaoSesi);
    }
}
