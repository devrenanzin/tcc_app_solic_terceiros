using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Cancelamento (UC18, RN06, RN10).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class CancelamentoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Cancelamento_mantem_a_etapa_registra_a_justificativa_e_encerra_a_passagem()
    {
        var demanda = _c.EmEntrevistas();

        demanda.Cancelar(_c.GestorNorte, "  Vaga suspensa pela área.  ", _c.Relogio);

        Assert.Equal(Etapa.Entrevistas, demanda.Etapa);
        Assert.Equal(StatusDemanda.Cancelado, demanda.Status);
        Assert.Equal("Vaga suspensa pela área.", demanda.MotivoCancelamento);
        Assert.Equal(StatusDemanda.Cancelado, demanda.Etapas[^1].Status);
        Assert.False(demanda.Etapas[^1].Aberta);

        var registro = demanda.Historico[^1];
        Assert.Equal(EventoDemanda.Cancelada, registro.Evento);
        Assert.Equal(StatusDemanda.EmAndamento, registro.StatusAnterior);
        Assert.Equal(StatusDemanda.Cancelado, registro.StatusNovo);
    }

    [Fact]
    public void Cancelamento_nao_apaga_nada_e_o_farol_mostra_cancelado()
    {
        var demanda = _c.Aprovada();
        var (etapas, historico) = (demanda.Etapas.Count, demanda.Historico.Count);

        demanda.Cancelar(_c.GestorNorte, "Justificativa.", _c.Relogio);

        Assert.Equal(etapas, demanda.Etapas.Count);
        Assert.Equal(historico + 1, demanda.Historico.Count);
        Assert.NotNull(demanda.Sla);
        Assert.Equal(Farol.Cancelado, demanda.ObterFarol(_c.Relogio, _c.Calendario));
    }

    [Fact]
    public void Admin_cancela_em_carater_excepcional()
    {
        var demanda = _c.EmExames();

        demanda.Cancelar(_c.Admin, "Operação excepcional.", _c.Relogio);

        Assert.True(demanda.Cancelada);
    }

    [Fact]
    public void Demanda_com_devolucao_pendente_pode_ser_cancelada()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Motivo.", _c.Relogio);

        demanda.Cancelar(_c.GestorNorte, "Solicitante desistiu.", _c.Relogio);

        Assert.Equal(Etapa.ValidacaoGestor, demanda.Etapa);
        Assert.True(demanda.Cancelada);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelamento_exige_justificativa(string justificativa)
    {
        var demanda = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => demanda.Cancelar(_c.GestorNorte, justificativa, _c.Relogio));
        Assert.False(demanda.Cancelada);
    }

    [Fact]
    public void Contratacao_concluida_nao_pode_ser_cancelada()
    {
        var demanda = _c.Finalizada();

        Assert.Throws<RegraNegocioException>(() => demanda.Cancelar(_c.Admin, "Justificativa.", _c.Relogio));
        Assert.Equal(StatusDemanda.Concluido, demanda.Status);
    }
}
