using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Quem pode executar cada ação: perfil, atividade e vínculo com o contrato (seções 4.5, 19 e 24).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class PermissoesTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void So_gestor_ativo_do_contrato_aprova()
    {
        foreach (var ator in new[] { _c.GestorSudeste, _c.GestorInativo, _c.SesiNorte, _c.Admin, _c.Solicitante })
        {
            var demanda = _c.Enviada();
            Assert.Throws<RegraNegocioException>(() => demanda.Aprovar(ator, Cenario.PrazoPadrao, _c.Relogio, _c.Calendario));
        }
    }

    [Fact]
    public void Gestor_com_varios_contratos_aprova_demanda_de_qualquer_um_deles()
    {
        var demanda = _c.Enviada();

        demanda.Aprovar(_c.GestorDosDois, Cenario.PrazoPadrao, _c.Relogio, _c.Calendario);

        Assert.Equal(_c.GestorDosDois.Id, demanda.GestorId);
    }

    [Fact]
    public void Gestor_de_outro_contrato_nao_devolve()
    {
        var demanda = _c.Enviada();

        Assert.Throws<RegraNegocioException>(() => demanda.DevolverPeloGestor(_c.GestorSudeste, "Motivo.", _c.Relogio));
    }

    [Fact]
    public void Sesi_de_outro_contrato_nao_aceita_nem_devolve()
    {
        var demanda = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => demanda.Aceitar(_c.SesiSudeste, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => demanda.DevolverPeloSesi(_c.SesiSudeste, TipoInconsistencia.Solicitante, "Motivo.", _c.Relogio));
    }

    [Fact]
    public void Gestor_nao_executa_acoes_do_sesi()
    {
        var demanda = _c.Aprovada();

        Assert.Throws<RegraNegocioException>(() => demanda.Aceitar(_c.GestorNorte, _c.Relogio));
    }

    [Fact]
    public void Qualquer_sesi_do_contrato_registra_as_etapas_seguintes_ao_aceite()
    {
        // SUPOSIÇÃO (S4)
        var demanda = _c.EmRecrutamento();

        demanda.RegistrarVaga(_c.OutroSesiNorte, "https://vagas.exemplo.ucl.br/1", _c.Relogio);
        demanda.IniciarEntrevistas(_c.OutroSesiNorte, _c.Relogio);

        Assert.Equal(Etapa.Entrevistas, demanda.Etapa);
        Assert.Equal(_c.SesiNorte.Id, demanda.ResponsavelSesiId);
    }

    [Fact]
    public void So_o_solicitante_que_criou_corrige()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Motivo.", _c.Relogio);

        Assert.Throws<RegraNegocioException>(() => _c.Corrigir(demanda, _c.OutroSolicitante));
        Assert.Throws<RegraNegocioException>(() => _c.Corrigir(demanda, _c.GestorNorte));
    }

    [Fact]
    public void Sesi_solicitante_e_gestor_de_outro_contrato_nao_cancelam()
    {
        foreach (var ator in new[] { _c.SesiNorte, _c.Solicitante, _c.GestorSudeste, _c.GestorInativo })
        {
            var demanda = _c.Aprovada();
            Assert.Throws<RegraNegocioException>(() => demanda.Cancelar(ator, "Justificativa.", _c.Relogio));
        }
    }
}
