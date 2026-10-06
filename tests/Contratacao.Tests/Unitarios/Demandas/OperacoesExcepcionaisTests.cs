using Contratacao.Tests.Unitarios.Anexos;
using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Cancelamento (UC18), operações excepcionais do Admin (UC20) e anexos depois do envio.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class OperacoesExcepcionaisTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void So_o_gestor_do_contrato_ve_o_cancelamento_e_so_antes_da_conclusao()
    {
        var demanda = _c.EmEntrevistas();

        Assert.Contains(AcaoDemanda.Cancelar, demanda.AcoesDisponiveis(_c.GestorNorte));
        foreach (var ator in new[] { _c.GestorSudeste, _c.SesiNorte, _c.Solicitante, _c.GestorInativo })
        {
            Assert.DoesNotContain(AcaoDemanda.Cancelar, demanda.AcoesDisponiveis(ator));
        }

        Assert.DoesNotContain(AcaoDemanda.Cancelar, _c.Finalizada().AcoesDisponiveis(_c.GestorNorte));
        demanda.Cancelar(_c.GestorNorte, "Vaga suspensa.", _c.Relogio);
        Assert.DoesNotContain(AcaoDemanda.Cancelar, demanda.AcoesDisponiveis(_c.GestorNorte));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Cancelamento_exige_justificativa(string justificativa)
        => Assert.Throws<RegraNegocioException>(() => _c.Aprovada().Cancelar(_c.GestorNorte, justificativa, _c.Relogio));

    [Fact]
    public void Admin_volta_de_exames_para_recrutamento_sem_reiniciar_o_sla()
    {
        var demanda = _c.EmExames();
        var sla = demanda.Sla;
        _c.Relogio.Avancar(TimeSpan.FromDays(2));

        demanda.ForcarTransicao(_c.Admin, Etapa.Recrutamento, "Candidato reprovado nos exames.", _c.Relogio);

        Assert.Equal((Etapa.Recrutamento, StatusDemanda.EmAndamento), (demanda.Etapa, demanda.Status));
        Assert.Same(sla, demanda.Sla);
        var exames = demanda.Etapas.Single(e => e.Etapa == Etapa.ExamesMedicos);
        Assert.Equal((StatusDemanda.EmAndamento, _c.Relogio.AgoraUtc), (exames.Status, exames.DataConclusao));
        Assert.True(demanda.Etapas.Single(e => e.Aberta).Etapa == Etapa.Recrutamento);
        var registro = demanda.Historico[^1];
        Assert.Equal((EventoDemanda.TransicaoForcada, "Candidato reprovado nos exames."), (registro.Evento, registro.Observacao));

        // A vaga continua registrada; o SESI segue a sequência a partir daqui.
        Assert.NotNull(demanda.Vaga);
        Assert.Equal([AcaoDemanda.IniciarEntrevistas], demanda.AcoesDisponiveis(_c.SesiNorte).Except([AcaoDemanda.Cancelar]));
    }

    [Fact]
    public void Admin_avanca_de_recrutamento_para_exames_concluindo_a_etapa()
    {
        var demanda = _c.EmRecrutamento();

        demanda.ForcarTransicao(_c.Admin, Etapa.ExamesMedicos, "Entrevistas feitas fora do sistema.", _c.Relogio);

        Assert.Equal(Etapa.ExamesMedicos, demanda.Etapa);
        Assert.Equal(StatusDemanda.Concluido, demanda.Etapas.Single(e => e.Etapa == Etapa.Recrutamento).Status);
    }

    [Fact]
    public void Transicao_forcada_so_pelo_admin_com_justificativa_e_entre_recrutamento_e_exames()
    {
        Assert.Throws<RegraNegocioException>(() => _c.EmEntrevistas().ForcarTransicao(_c.GestorNorte, Etapa.Recrutamento, "Motivo.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.EmEntrevistas().ForcarTransicao(_c.Admin, Etapa.Recrutamento, " ", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.EmEntrevistas().ForcarTransicao(_c.Admin, Etapa.Entrevistas, "Motivo.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.EmEntrevistas().ForcarTransicao(_c.Admin, Etapa.Contratacao, "Motivo.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.EmEntrevistas().ForcarTransicao(_c.Admin, Etapa.ValidacaoSesi, "Motivo.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.Aprovada().ForcarTransicao(_c.Admin, Etapa.Recrutamento, "Motivo.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => _c.Finalizada().ForcarTransicao(_c.Admin, Etapa.Recrutamento, "Motivo.", _c.Relogio));

        var cancelada = _c.EmEntrevistas();
        cancelada.Cancelar(_c.Admin, "Operação excepcional.", _c.Relogio);
        Assert.Throws<RegraNegocioException>(() => cancelada.ForcarTransicao(_c.Admin, Etapa.Recrutamento, "Motivo.", _c.Relogio));
    }

    [Fact]
    public void Admin_altera_data_congelada_com_justificativa_no_historico()
    {
        var demanda = _c.EmEntrevistas();
        var recrutamento = demanda.Etapas.Single(e => e.Etapa == Etapa.Recrutamento);
        var nova = recrutamento.DataInicio.AddMinutes(30);
        _c.Relogio.Avancar(TimeSpan.FromHours(1));

        var (campo, anterior, novo) = demanda.AlterarDataExcepcional(
            _c.Admin, DataAlteravel.ConclusaoEtapa, recrutamento.Id, nova, "Data registrada com atraso.", _c.Relogio, _c.Calendario);

        Assert.Equal(nova, recrutamento.DataConclusao);
        Assert.Equal("Recrutamento.DataConclusao", campo);
        var alteracao = Assert.Single(demanda.Alteracoes);
        Assert.Equal((anterior, novo, "Data registrada com atraso."), (alteracao.ValorAnterior, alteracao.NovoValor, alteracao.Justificativa));
        Assert.Equal(Web.Domain.Usuarios.Perfil.Admin, alteracao.PerfilUsuario);
    }

    [Fact]
    public void Novo_inicio_do_sla_recalcula_a_data_limite_com_o_mesmo_prazo()
    {
        var demanda = _c.Aprovada();
        var inicio = demanda.Sla!.InicioUtc;
        _c.Relogio.Avancar(TimeSpan.FromDays(3));

        demanda.AlterarDataExcepcional(_c.Admin, DataAlteravel.InicioSla, null, inicio.AddDays(2), "Aprovação registrada no dia errado.",
            _c.Relogio, _c.Calendario);

        Assert.Equal(inicio.AddDays(2), demanda.Sla!.InicioUtc);
        Assert.Equal(Cenario.PrazoPadrao, demanda.Sla.PrazoDias);
        Assert.Equal(_c.Calendario.DataLocal(inicio).AddDays(2 + Cenario.PrazoPadrao), demanda.Sla.DataLimite);
        Assert.Equal(["Sla.DataLimite", "Sla.Inicio"], demanda.Alteracoes.Select(a => a.Campo).Order());
    }

    [Fact]
    public void Conclusao_da_contratacao_acompanha_a_data_de_finalizacao()
    {
        var demanda = _c.Finalizada();
        var contratacao = demanda.Etapas.Single(e => e.Etapa == Etapa.Contratacao);
        var nova = contratacao.DataInicio;
        _c.Relogio.Avancar(TimeSpan.FromDays(1));

        demanda.AlterarDataExcepcional(_c.Admin, DataAlteravel.ConclusaoEtapa, contratacao.Id, nova, "Ajuste.", _c.Relogio, _c.Calendario);

        Assert.Equal(nova, demanda.DataFinalizacao);
    }

    [Fact]
    public void Alteracao_de_data_recusa_futuro_antes_do_envio_conclusao_antes_do_inicio_e_quem_nao_e_admin()
    {
        var demanda = _c.ComVaga();
        var recrutamento = demanda.Etapas.Single(e => e.Etapa == Etapa.Recrutamento);
        var agora = _c.Relogio.AgoraUtc;

        void Tentar(Web.Domain.Usuarios.Ator ator, DataAlteravel data, Guid? id, DateTime nova, string justificativa = "Motivo.")
            => Assert.Throws<RegraNegocioException>(() => demanda.AlterarDataExcepcional(ator, data, id, nova, justificativa, _c.Relogio, _c.Calendario));

        Tentar(_c.GestorNorte, DataAlteravel.AberturaVaga, null, agora);
        Tentar(_c.Admin, DataAlteravel.AberturaVaga, null, agora, "");
        Tentar(_c.Admin, DataAlteravel.AberturaVaga, null, agora.AddMinutes(1));
        Tentar(_c.Admin, DataAlteravel.AberturaVaga, null, demanda.DataCriacao.AddMinutes(-1));
        Tentar(_c.Admin, DataAlteravel.ConclusaoEtapa, recrutamento.Id, agora); // etapa em curso
        Tentar(_c.Admin, DataAlteravel.InicioEtapa, Guid.NewGuid(), agora);
        Assert.Empty(demanda.Alteracoes);
    }

    [Fact]
    public void Conclusao_nao_fica_antes_do_inicio_da_etapa()
    {
        var demanda = _c.Enviada();
        _c.Relogio.Avancar(TimeSpan.FromHours(1));
        demanda.Aprovar(_c.GestorNorte, Cenario.PrazoPadrao, _c.Relogio, _c.Calendario);
        _c.Relogio.Avancar(TimeSpan.FromHours(1));
        demanda.Aceitar(_c.SesiNorte, _c.Relogio);
        var validacaoSesi = demanda.Etapas.Single(e => e.Etapa == Etapa.ValidacaoSesi);

        var erro = Assert.Throws<RegraNegocioException>(() => demanda.AlterarDataExcepcional(_c.Admin, DataAlteravel.ConclusaoEtapa,
            validacaoSesi.Id, validacaoSesi.DataInicio.AddMinutes(-1), "Motivo.", _c.Relogio, _c.Calendario));

        Assert.Contains("anterior ao início", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Anexo_depois_do_envio_so_na_correcao_e_pelo_solicitante()
    {
        var arquivo = RegraArquivo.Conferir("novo-vp2.pdf", RegraArquivoTests.Pdf.Length, RegraArquivoTests.Pdf);
        var demanda = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => Anexo.NaCorrecao(demanda, _c.Solicitante, arquivo, "x", CategoriaAnexo.Geral, _c.Relogio.AgoraUtc));

        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Solicitante, "VP-2 ilegível.", _c.Relogio);
        Assert.Throws<RegraNegocioException>(() => Anexo.NaCorrecao(demanda, _c.OutroSolicitante, arquivo, "x", CategoriaAnexo.Geral, _c.Relogio.AgoraUtc));
        Assert.Throws<RegraNegocioException>(() => Anexo.NaCorrecao(demanda, _c.SesiNorte, arquivo, "x", CategoriaAnexo.Geral, _c.Relogio.AgoraUtc));

        var anexo = Anexo.NaCorrecao(demanda, _c.Solicitante, arquivo, "2026/09/x.pdf", CategoriaAnexo.DeAcordoVP2, _c.Relogio.AgoraUtc);
        Assert.Equal((Etapa.ValidacaoSesi, CategoriaAnexo.DeAcordoVP2), (anexo.Etapa, anexo.Categoria));
    }
}
