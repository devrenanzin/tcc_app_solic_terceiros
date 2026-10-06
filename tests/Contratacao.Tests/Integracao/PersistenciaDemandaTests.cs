using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Demanda gravada e lida do SQL Server: fluxo completo, concorrência, não exclusão e restrições.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class PersistenciaDemandaTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private static readonly ICalendarioSla Calendario = new CalendarioBrasilia();
    private static int _sequencial;

    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Demanda_percorre_o_fluxo_inteiro_e_volta_igual_do_banco()
    {
        var (solicitante, gestor, sesi) = await CriarAtoresAsync();
        var id = await EnviarAsync(solicitante);
        var dados = await banco.DadosAsync();
        var referencias = await banco.ReferenciasAsync(dados);

        await ExecutarAsync(id, d => d.DevolverPeloGestor(gestor, "Corrigir a localidade.", banco.Relogio));
        await ExecutarAsync(id, d => d.Corrigir(solicitante, dados with { LocalidadeVaga = "Serra" }, referencias, banco.Relogio));
        await ExecutarAsync(id, d => d.Aprovar(gestor, 45, banco.Relogio, Calendario));
        await ExecutarAsync(id, d => d.DevolverPeloSesi(sesi, TipoInconsistencia.Contratual, "Coletor de custo errado.", banco.Relogio));
        await ExecutarAsync(id, d => d.Corrigir(solicitante, dados with { LocalidadeVaga = "Serra", Racs = new HashSet<Guid>() }, referencias, banco.Relogio));
        await ExecutarAsync(id, d => d.Aprovar(gestor, 30, banco.Relogio, Calendario));
        await ExecutarAsync(id, d => d.Aceitar(sesi, banco.Relogio));
        await ExecutarAsync(id, d => d.RegistrarVaga(sesi, "https://vagas.exemplo.ucl.br/42", banco.Relogio));
        await ExecutarAsync(id, d => d.IniciarEntrevistas(sesi, banco.Relogio));
        await ExecutarAsync(id, d => d.IniciarExames(sesi, banco.Relogio));
        await ExecutarAsync(id, d => d.Finalizar(sesi, banco.Relogio));

        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);

        Assert.Equal(Etapa.Contratacao, demanda.Etapa);
        Assert.Equal(StatusDemanda.Concluido, demanda.Status);
        Assert.Equal(IdsFixos.ContratadaSesi, demanda.ContratadaId);
        Assert.Equal(gestor.Id, demanda.GestorId);
        Assert.Equal(sesi.Id, demanda.ResponsavelSesiId);
        Assert.Equal(DateTimeKind.Utc, demanda.DataFinalizacao!.Value.Kind);

        // A segunda aprovação, com prazo padrão de 30, não mexeu no SLA iniciado com 45 (RN05, RN07).
        Assert.Equal(45, demanda.Sla!.PrazoDias);
        Assert.Equal(DateTimeKind.Utc, demanda.Sla.InicioUtc.Kind);
        Assert.Equal(Calendario.DataLocal(demanda.Sla.InicioUtc).AddDays(45), demanda.Sla.DataLimite);

        Assert.Equal(2, demanda.Correcoes.Count);
        Assert.All(demanda.Correcoes, c => Assert.NotNull(c.DataResolucao));
        Assert.Contains(demanda.Correcoes, c => c is { Origem: OrigemCorrecao.Sesi, Tipo: TipoInconsistencia.Contratual });
        Assert.Contains(demanda.Correcoes, c => c is { Origem: OrigemCorrecao.Gestor, Tipo: null });

        Assert.All(demanda.Etapas, e => Assert.False(e.Aberta));
        Assert.Equal(2, demanda.Etapas.Count(e => e.Etapa == Etapa.ValidacaoGestor));
        Assert.Equal("https://vagas.exemplo.ucl.br/42", demanda.Vaga!.LinkExterno);
        Assert.Equal(12, demanda.Historico.Count);
        Assert.Equal("Serra", demanda.LocalidadeVaga);
        Assert.Empty(demanda.Racs);
        Assert.Equal(["LocalidadeVaga", "Racs"], demanda.Alteracoes.Select(a => a.Campo).Order());
        Assert.Equal(EventoDemanda.ContratacaoFinalizada, demanda.Historico.OrderBy(h => h.DataHora).Last().Evento);
        Assert.Equal(Farol.Verde, demanda.ObterFarol(banco.Relogio, Calendario));

        var etapaGravada = await contexto.Database
            .SqlQuery<Guid>($"SELECT EtapaAtualId AS Value FROM Demanda WHERE Id = {id}")
            .SingleAsync(Cancelamento);
        Assert.Equal(IdsFixos.Etapas[Etapa.Contratacao], etapaGravada);
    }

    [Fact]
    public async Task Duas_acoes_simultaneas_na_mesma_demanda_nao_se_sobrescrevem()
    {
        var (solicitante, gestor, _) = await CriarAtoresAsync();
        var id = await EnviarAsync(solicitante);

        await using var primeiro = banco.NovoContexto();
        await using var segundo = banco.NovoContexto();
        var naPrimeiraTela = await BancoFixture.CarregarDemandaAsync(primeiro, id);
        var naSegundaTela = await BancoFixture.CarregarDemandaAsync(segundo, id);

        naPrimeiraTela.Aprovar(gestor, 45, banco.Relogio, Calendario);
        await primeiro.SaveChangesAsync(Cancelamento);

        naSegundaTela.DevolverPeloGestor(gestor, "Motivo.", banco.Relogio);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => segundo.SaveChangesAsync(Cancelamento));
    }

    [Fact]
    public async Task Nenhuma_chave_estrangeira_exclui_em_cascata()
    {
        await using var contexto = banco.NovoContexto();

        var total = await contexto.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.foreign_keys").SingleAsync(Cancelamento);
        var comAcao = await contexto.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.foreign_keys WHERE delete_referential_action <> 0")
            .SingleAsync(Cancelamento);

        Assert.True(total > 30);
        Assert.Equal(0, comAcao);
    }

    [Fact]
    public async Task Banco_recusa_excluir_demanda_com_historico()
    {
        var (solicitante, _, _) = await CriarAtoresAsync();
        var id = await EnviarAsync(solicitante);

        await using var contexto = banco.NovoContexto();
        var erro = await Assert.ThrowsAsync<SqlException>(
            () => contexto.Database.ExecuteSqlAsync($"DELETE FROM Demanda WHERE Id = {id}", Cancelamento));

        Assert.Contains("REFERENCE", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await contexto.Demandas.AnyAsync(d => d.Id == id, Cancelamento));
    }

    [Fact]
    public async Task Banco_recusa_email_fora_do_dominio_ucl()
    {
        await using var contexto = banco.NovoContexto();
        var perfil = IdsFixos.Perfis[Perfil.Solicitante];

        var erro = await Assert.ThrowsAsync<SqlException>(() => contexto.Database.ExecuteSqlAsync($"""
            INSERT INTO Usuario (Id, PerfilId, Nome, Email, Login, Ativo, DataCadastro, SenhaHash)
            VALUES ({Guid.NewGuid()}, {perfil}, {"Fulano"}, {"fulano@exemplo.com"}, {"fulano"}, 1, SYSUTCDATETIME(), {"hash"})
            """, Cancelamento));

        Assert.Contains("CK_Usuario_Email_Dominio", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Alteracao_de_campo_guarda_usuario_perfil_e_ip()
    {
        var (solicitante, _, _) = await CriarAtoresAsync();
        var id = await EnviarAsync(solicitante);

        await using (var contexto = banco.NovoContexto())
        {
            contexto.HistoricosAlteracao.Add(new Contratacao.Web.Domain.Auditoria.HistoricoAlteracao(
                id, solicitante, "LocalidadeVaga", "Vitória", "Serra", null, banco.Relogio.AgoraUtc));
            await contexto.SaveChangesAsync(Cancelamento);
        }

        await using var leitura = banco.NovoContexto();
        var alteracao = await leitura.HistoricosAlteracao.SingleAsync(h => h.DemandaId == id, Cancelamento);
        Assert.Equal(solicitante.Id, alteracao.UsuarioId);
        Assert.Equal(Perfil.Solicitante, alteracao.PerfilUsuario);
        Assert.Equal("10.0.0.1", alteracao.EnderecoIp);
        Assert.Equal(("Vitória", "Serra"), (alteracao.ValorAnterior, alteracao.NovoValor));
    }

    [Theory]
    [InlineData("Usuario", "Login", "nvarchar", "NO")]
    [InlineData("HistoricoAlteracao", "PerfilUsuario", "nvarchar", "NO")]
    [InlineData("HistoricoAlteracao", "EnderecoIp", "varchar", "YES")]
    [InlineData("Demanda", "DataLimiteSLA", "date", "YES")]
    [InlineData("Demanda", "Numero", "nvarchar", "NO")]
    [InlineData("Demanda", "GestorId", "uniqueidentifier", "YES")]
    [InlineData("Demanda", "DataEnvio", "datetime2", "NO")]
    [InlineData("Demanda", "RowVersion", "timestamp", "NO")]
    [InlineData("Contrato", "Numero", "varchar", "NO")]
    [InlineData("Contratada", "CNPJ", "char", "YES")]
    [InlineData("SolicitacaoCorrecao", "Tipo", "varchar", "YES")]
    [InlineData("Usuario", "ContratoId", "uniqueidentifier", "YES")]
    [InlineData("GestorContrato", "ContratoId", "uniqueidentifier", "NO")]
    [InlineData("Demanda", "CustoTotal", "decimal", "NO")]
    [InlineData("Demanda", "QuantidadeSolicitada", "smallint", "NO")]
    [InlineData("Demanda", "PeriodoTemporarioMeses", "smallint", "YES")]
    [InlineData("Demanda", "CategoriaCnh", "varchar", "YES")]
    [InlineData("Demanda", "OrdemServicoId", "uniqueidentifier", "NO")]
    [InlineData("OrdemServico", "Numero", "nvarchar", "NO")]
    [InlineData("Demanda", "ItemQqpId", "uniqueidentifier", "NO")]
    [InlineData("Demanda", "Observacoes", "nvarchar", "YES")]
    [InlineData("DemandaRac", "RacId", "uniqueidentifier", "NO")]
    public async Task Colunas_seguem_o_modelo_fisico(string tabela, string coluna, string tipo, string anulavel)
    {
        await using var contexto = banco.NovoContexto();

        var encontrada = await contexto.Database.SqlQuery<string>($"""
            SELECT DATA_TYPE + '|' + IS_NULLABLE AS Value FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = {tabela} AND COLUMN_NAME = {coluna}
            """).SingleAsync(Cancelamento);

        Assert.Equal($"{tipo}|{anulavel}", encontrada);
    }

    private async Task<(Ator Solicitante, Ator Gestor, Ator Sesi)> CriarAtoresAsync()
        => (await banco.InserirUsuarioAsync(Perfil.Solicitante),
            await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte),
            await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte));

    private async Task<Guid> EnviarAsync(Ator solicitante)
    {
        var dados = await banco.DadosAsync();
        var referencias = await banco.ReferenciasAsync(dados);
        var numero = NumeroDemanda.Formatar(2026, Interlocked.Increment(ref _sequencial));

        await using var contexto = banco.NovoContexto();
        var demanda = Demanda.Enviar(numero, solicitante, dados, referencias, possuiDeAcordoVp2: true, banco.Relogio);
        contexto.Demandas.Add(demanda);
        await contexto.SaveChangesAsync(Cancelamento);
        return demanda.Id;
    }

    /// <summary>Cada ação num contexto novo, como numa requisição web, uma hora depois da anterior.</summary>
    private async Task ExecutarAsync(Guid id, Action<Demanda> acao)
    {
        banco.Relogio.Avancar(TimeSpan.FromHours(1));
        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        acao(demanda);
        await contexto.SaveChangesAsync(Cancelamento);
    }
}
