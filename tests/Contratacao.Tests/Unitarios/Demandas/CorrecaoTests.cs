using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Devoluções e correções (RN01, RN02, RN02a, RN02c, RN05).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class CorrecaoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Devolucao_pelo_gestor_registra_a_correcao_e_a_correcao_volta_a_validacao_do_gestor()
    {
        var demanda = _c.Enviada();

        demanda.DevolverPeloGestor(_c.GestorNorte, "  Corrigir a localidade.  ", _c.Relogio);

        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.AguardandoCorrecao, demanda.Status);
        var correcao = Assert.Single(demanda.Correcoes);
        Assert.Equal(OrigemCorrecao.Gestor, correcao.Origem);
        Assert.Null(correcao.Tipo);
        Assert.Equal("Corrigir a localidade.", correcao.Motivo);
        Assert.True(correcao.Pendente);

        _c.Relogio.Avancar(TimeSpan.FromHours(2));
        _c.Corrigir(demanda);

        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
        Assert.Equal(_c.Relogio.AgoraUtc, correcao.DataResolucao);
    }

    [Fact]
    public void Inconsistencia_do_solicitante_corrigida_volta_direto_ao_sesi_sem_mexer_no_sla()
    {
        var demanda = _c.Aprovada();
        var sla = demanda.Sla!;

        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Solicitante, "Descrição incompleta.", _c.Relogio);
        _c.Relogio.Avancar(TimeSpan.FromDays(3));
        _c.Corrigir(demanda);

        Assert.Equal(Etapa.ValidacaoSesi, demanda.Etapa);
        Assert.Equal(StatusDemanda.AguardandoResponsavel, demanda.Status);
        Assert.Same(sla, demanda.Sla);
        Assert.Equal(TipoInconsistencia.Solicitante, demanda.Correcoes[0].Tipo);
        Assert.Equal(OrigemCorrecao.Sesi, demanda.Correcoes[0].Origem);
    }

    [Fact]
    public void Inconsistencia_contratual_corrigida_passa_de_novo_pelo_gestor_sem_reiniciar_o_sla()
    {
        var demanda = _c.Aprovada();
        var (inicio, limite) = (demanda.Sla!.InicioUtc, demanda.Sla.DataLimite);

        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Contratual, "Coletor de custo errado.", _c.Relogio);
        _c.Relogio.Avancar(TimeSpan.FromDays(5));
        _c.Corrigir(demanda);

        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
        var validacaoSesi = demanda.Etapas.Single(e => e.Etapa == Etapa.ValidacaoSesi);
        Assert.False(validacaoSesi.Aberta);
        Assert.Equal(StatusDemanda.AguardandoCorrecao, validacaoSesi.Status);

        _c.Relogio.Avancar(TimeSpan.FromDays(1));
        demanda.Aprovar(_c.GestorNorte, 30, _c.Relogio, _c.Calendario);

        Assert.Equal(Etapa.ValidacaoSesi, demanda.Etapa);
        Assert.Equal(StatusDemanda.AguardandoResponsavel, demanda.Status);
        Assert.Equal(inicio, demanda.Sla.InicioUtc);
        Assert.Equal(limite, demanda.Sla.DataLimite);
        Assert.Equal(Cenario.PrazoPadrao, demanda.Sla.PrazoDias);
    }

    [Fact]
    public void Troca_de_corredor_que_muda_o_contrato_volta_ao_gestor_do_novo_contrato()
    {
        var demanda = _c.Aprovada();
        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Solicitante, "Corredor errado.", _c.Relogio);

        _c.Corrigir(demanda, paraSudeste: true);

        Assert.Equal(Cenario.ContratoSudeste.Id, demanda.ContratoId);
        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
        Assert.Throws<RegraNegocioException>(() => demanda.Aprovar(_c.GestorNorte, Cenario.PrazoPadrao, _c.Relogio, _c.Calendario));

        demanda.Aprovar(_c.GestorSudeste, Cenario.PrazoPadrao, _c.Relogio, _c.Calendario);

        Assert.Equal(_c.GestorSudeste.Id, demanda.GestorId);
        Assert.Throws<RegraNegocioException>(() => demanda.Aceitar(_c.SesiNorte, _c.Relogio));
        demanda.Aceitar(_c.SesiSudeste, _c.Relogio);
    }

    [Fact]
    public void Correcao_na_validacao_do_gestor_atualiza_o_contrato_e_fica_com_o_gestor()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Corredor errado.", _c.Relogio);

        _c.Corrigir(demanda, paraSudeste: true);

        Assert.Equal(Cenario.ContratoSudeste.Id, demanda.ContratoId);
        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
    }

    [Fact]
    public void Cada_devolucao_gera_sua_propria_solicitacao_de_correcao()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Primeiro motivo.", _c.Relogio);
        _c.Corrigir(demanda);
        demanda.DevolverPeloGestor(_c.GestorNorte, "Segundo motivo.", _c.Relogio);

        Assert.Equal(2, demanda.Correcoes.Count);
        Assert.False(demanda.Correcoes[0].Pendente);
        Assert.True(demanda.Correcoes[1].Pendente);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devolucao_exige_motivo(string motivo)
    {
        var enviada = _c.Enviada();
        var aprovada = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => enviada.DevolverPeloGestor(_c.GestorNorte, motivo, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => aprovada.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Solicitante, motivo, _c.Relogio));
        Assert.Empty(enviada.Correcoes);
        Assert.Empty(aprovada.Correcoes);
    }

    [Fact]
    public void Motivo_tem_no_maximo_1000_caracteres()
    {
        var demanda = _c.Enviada();

        Assert.Throws<RegraNegocioException>(() => demanda.DevolverPeloGestor(_c.GestorNorte, new string('x', 1001), _c.Relogio));
        demanda.DevolverPeloGestor(_c.GestorNorte, new string('x', 1000), _c.Relogio);
    }

    [Fact]
    public void Devolucao_do_sesi_exige_tipo_valido()
    {
        var demanda = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => demanda.DevolverPeloSesi(_c.SesiNorte, (TipoInconsistencia)99, "Motivo.", _c.Relogio));
    }
}
