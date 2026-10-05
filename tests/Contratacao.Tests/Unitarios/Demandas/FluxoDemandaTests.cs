using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Caminho feliz e efeitos de cada transição permitida (seções 5–7 e 24).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class FluxoDemandaTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Envio_cria_a_demanda_em_validacao_do_gestor_com_a_etapa_solicitacao_concluida()
    {
        var demanda = _c.Enviada();

        Assert.Equal("2026-000123", demanda.Numero);
        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
        Assert.Equal(_c.Relogio.AgoraUtc, demanda.DataCriacao);
        Assert.Equal(_c.Relogio.AgoraUtc, demanda.DataEnvio);
        Assert.Null(demanda.GestorId);
        Assert.Null(demanda.Sla);

        var solicitacao = demanda.Etapas[0];
        Assert.Equal(Etapa.Solicitacao, solicitacao.Etapa);
        Assert.Equal(StatusDemanda.Concluido, solicitacao.Status);
        Assert.NotNull(solicitacao.DataConclusao);
        Assert.True(demanda.Etapas[1].Aberta);
    }

    [Theory]
    [InlineData("2026-123")]
    [InlineData("26-000123")]
    [InlineData("2026-000000")]
    [InlineData("")]
    public void Envio_recusa_numero_fora_do_formato(string numero)
        => Assert.Throws<RegraNegocioException>(() => _c.Enviar(numero));

    [Fact]
    public void Envio_so_por_solicitante_ativo()
    {
        Assert.Throws<RegraNegocioException>(() => _c.Enviar(Cenario.Numero, _c.GestorNorte));
        Assert.Throws<RegraNegocioException>(() => _c.Enviar(Cenario.Numero, _c.Solicitante with { Ativo = false }));
    }

    [Fact]
    public void Aprovacao_define_o_gestor_inicia_o_sla_e_leva_a_validacao_sesi()
    {
        var demanda = _c.Aprovada();

        Assert.Equal(Etapa.ValidacaoSesi, demanda.Etapa);
        Assert.Equal(StatusDemanda.AguardandoResponsavel, demanda.Status);
        Assert.Equal(_c.GestorNorte.Id, demanda.GestorId);
        Assert.NotNull(demanda.Sla);
        Assert.Equal(_c.Relogio.AgoraUtc, demanda.Sla.InicioUtc);
        Assert.Equal(StatusDemanda.Concluido, demanda.Etapas[1].Status);
        Assert.Equal(_c.GestorNorte.Id, demanda.Etapas[1].UsuarioResponsavelId);
    }

    [Fact]
    public void Aceite_define_o_responsavel_sesi()
    {
        var demanda = _c.EmRecrutamento();

        Assert.Equal(Etapa.Recrutamento, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAndamento, demanda.Status);
        Assert.Equal(_c.SesiNorte.Id, demanda.ResponsavelSesiId);
    }

    [Fact]
    public void Registro_da_vaga_e_evento_e_nao_muda_etapa_nem_status()
    {
        var demanda = _c.ComVaga();

        Assert.Equal(Etapa.Recrutamento, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAndamento, demanda.Status);
        Assert.Equal("https://vagas.exemplo.ucl.br/123", demanda.Vaga!.LinkExterno);
        Assert.Equal(EventoDemanda.VagaAberta, demanda.Historico[^1].Evento);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("vagas.exemplo.ucl.br/123")]
    [InlineData("ftp://vagas.exemplo.ucl.br/123")]
    [InlineData("javascript:alert(1)")]
    public void Vaga_exige_link_http_ou_https_valido(string link)
    {
        var demanda = _c.EmRecrutamento();

        Assert.Throws<RegraNegocioException>(() => demanda.RegistrarVaga(_c.SesiNorte, link, _c.Relogio));
        Assert.Null(demanda.Vaga);
    }

    [Fact]
    public void Finalizacao_conclui_a_contratacao_e_todas_as_etapas()
    {
        var demanda = _c.Finalizada();

        Assert.Equal(Etapa.Contratacao, demanda.Etapa);
        Assert.Equal(StatusDemanda.Concluido, demanda.Status);
        Assert.Equal(_c.Relogio.AgoraUtc, demanda.DataFinalizacao);
        Assert.Equal(
            [Etapa.Solicitacao, Etapa.ValidacaoGestor, Etapa.ValidacaoSesi, Etapa.Recrutamento,
             Etapa.Entrevistas, Etapa.ExamesMedicos, Etapa.Contratacao],
            demanda.Etapas.Select(e => e.Etapa));
        Assert.All(demanda.Etapas, e =>
        {
            Assert.Equal(StatusDemanda.Concluido, e.Status);
            Assert.NotNull(e.DataConclusao);
        });
    }

    [Fact]
    public void Historico_registra_cada_evento_com_usuario_perfil_ip_e_situacoes()
    {
        var demanda = _c.Finalizada();

        Assert.Equal(
            [EventoDemanda.Enviada, EventoDemanda.AprovadaPeloGestor, EventoDemanda.AceitaPeloSesi,
             EventoDemanda.VagaAberta, EventoDemanda.EntrevistasIniciadas, EventoDemanda.ExamesIniciados,
             EventoDemanda.ContratacaoFinalizada],
            demanda.Historico.Select(h => h.Evento));

        var aprovacao = demanda.Historico[1];
        Assert.Equal(_c.GestorNorte.Id, aprovacao.UsuarioId);
        Assert.Equal(Perfil.Gestor, aprovacao.PerfilUsuario);
        Assert.Equal("10.0.0.1", aprovacao.EnderecoIp);
        Assert.Equal(Etapa.ValidacaoGestor, aprovacao.EtapaAnterior);
        Assert.Equal(StatusDemanda.EmAnalise, aprovacao.StatusAnterior);
        Assert.Equal(Etapa.ValidacaoSesi, aprovacao.EtapaNova);
        Assert.Equal(StatusDemanda.AguardandoResponsavel, aprovacao.StatusNovo);
    }

    [Fact]
    public void Data_de_etapa_concluida_nao_pode_ser_alterada()
    {
        var demanda = _c.Aprovada();
        var concluida = demanda.Etapas[0];

        Assert.Throws<RegraNegocioException>(() => concluida.Encerrar(StatusDemanda.Concluido, _c.Relogio.AgoraUtc.AddDays(1)));
        Assert.Throws<RegraNegocioException>(() => concluida.AtualizarStatus(StatusDemanda.EmAndamento));
    }
}
