using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Processo SESI (UC08–11): sequência obrigatória, quem conduz, responsáveis e congelamento de datas.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class ProcessoSesiTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Cada_etapa_oferece_so_o_proximo_passo_ao_sesi_do_contrato()
    {
        Assert.Equal([AcaoDemanda.RegistrarVaga], _c.EmRecrutamento().AcoesDisponiveis(_c.SesiNorte));
        Assert.Equal([AcaoDemanda.IniciarEntrevistas], _c.ComVaga().AcoesDisponiveis(_c.SesiNorte));
        Assert.Equal([AcaoDemanda.IniciarExames], _c.EmEntrevistas().AcoesDisponiveis(_c.SesiNorte));
        Assert.Equal([AcaoDemanda.Finalizar], _c.EmExames().AcoesDisponiveis(_c.SesiNorte));
        Assert.Empty(_c.Finalizada().AcoesDisponiveis(_c.SesiNorte));

        // Qualquer SESI ativo do contrato conduz (Cliente, S4); o de outro contrato e os demais perfis, não.
        Assert.Equal([AcaoDemanda.RegistrarVaga], _c.EmRecrutamento().AcoesDisponiveis(_c.OutroSesiNorte));
        foreach (var ator in new[] { _c.SesiSudeste, _c.Solicitante, _c.Admin, _c.SesiNorte with { Ativo = false } })
        {
            Assert.Empty(_c.EmEntrevistas().AcoesDisponiveis(ator));
        }

        Assert.Equal([AcaoDemanda.Cancelar], _c.EmEntrevistas().AcoesDisponiveis(_c.GestorNorte)); // o Gestor só cancela
    }

    [Fact]
    public void Demanda_cancelada_nao_oferece_passos_do_sesi()
    {
        var demanda = _c.ComVaga();
        demanda.Cancelar(_c.GestorNorte, "Vaga suspensa.", _c.Relogio);

        Assert.Empty(demanda.AcoesDisponiveis(_c.SesiNorte));
        Assert.Throws<RegraNegocioException>(() => demanda.IniciarEntrevistas(_c.SesiNorte, _c.Relogio));
    }

    [Fact]
    public void Nao_se_pula_etapa_nem_se_repete_a_vaga()
    {
        var recrutamento = _c.EmRecrutamento();
        Assert.Throws<RegraNegocioException>(() => recrutamento.IniciarEntrevistas(_c.SesiNorte, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => recrutamento.IniciarExames(_c.SesiNorte, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => recrutamento.Finalizar(_c.SesiNorte, _c.Relogio));

        var comVaga = _c.ComVaga();
        Assert.Throws<RegraNegocioException>(() => comVaga.RegistrarVaga(_c.SesiNorte, "https://vagas.exemplo.ucl.br/2", _c.Relogio));

        var entrevistas = _c.EmEntrevistas();
        Assert.Throws<RegraNegocioException>(() => entrevistas.Finalizar(_c.SesiNorte, _c.Relogio));
    }

    [Theory]
    [InlineData("")]
    [InlineData("vagas.exemplo.ucl.br/1")]
    [InlineData("ftp://vagas.exemplo.ucl.br/1")]
    [InlineData("javascript:alert(1)")]
    public void Vaga_exige_link_http_ou_https(string link)
        => Assert.Throws<RegraNegocioException>(() => _c.EmRecrutamento().RegistrarVaga(_c.SesiNorte, link, _c.Relogio));

    [Fact]
    public void Cada_etapa_guarda_inicio_conclusao_e_quem_a_concluiu()
    {
        var demanda = _c.EmRecrutamento();
        var horario = new Dictionary<string, DateTime>();

        _c.Relogio.Avancar(TimeSpan.FromDays(1));
        demanda.RegistrarVaga(_c.SesiNorte, "https://vagas.exemplo.ucl.br/7", _c.Relogio);
        _c.Relogio.Avancar(TimeSpan.FromDays(2));
        demanda.IniciarEntrevistas(_c.OutroSesiNorte, _c.Relogio);
        horario["Recrutamento"] = _c.Relogio.AgoraUtc;
        _c.Relogio.Avancar(TimeSpan.FromDays(3));
        demanda.IniciarExames(_c.SesiNorte, _c.Relogio);
        horario["Entrevistas"] = _c.Relogio.AgoraUtc;
        _c.Relogio.Avancar(TimeSpan.FromDays(4));
        demanda.Finalizar(_c.OutroSesiNorte, _c.Relogio);
        horario["Exames"] = _c.Relogio.AgoraUtc;

        EtapaDemanda Passagem(Etapa etapa) => demanda.Etapas.Single(e => e.Etapa == etapa);
        Assert.Equal((horario["Recrutamento"], _c.OutroSesiNorte.Id), (Passagem(Etapa.Recrutamento).DataConclusao, Passagem(Etapa.Recrutamento).UsuarioResponsavelId));
        Assert.Equal((horario["Entrevistas"], _c.SesiNorte.Id), (Passagem(Etapa.Entrevistas).DataConclusao, Passagem(Etapa.Entrevistas).UsuarioResponsavelId));
        Assert.Equal((horario["Exames"], _c.OutroSesiNorte.Id), (Passagem(Etapa.ExamesMedicos).DataConclusao, Passagem(Etapa.ExamesMedicos).UsuarioResponsavelId));
        Assert.Equal(horario["Exames"], Passagem(Etapa.Contratacao).DataConclusao);
        Assert.Equal(horario["Recrutamento"], Passagem(Etapa.Entrevistas).DataInicio);

        Assert.All(demanda.Etapas, e => Assert.False(e.Aberta));
        Assert.Equal(_c.SesiNorte.Id, demanda.ResponsavelSesiId); // quem aceitou continua o responsável SESI
        Assert.Equal(horario["Exames"], demanda.DataFinalizacao);
        Assert.Equal(Farol.Verde, demanda.ObterFarol(_c.Relogio, _c.Calendario));
        Assert.Equal(EventoDemanda.ContratacaoFinalizada, demanda.Historico[^1].Evento);
    }

    [Fact]
    public void Etapa_concluida_tem_data_status_e_responsavel_congelados()
    {
        var demanda = _c.EmEntrevistas();
        var recrutamento = demanda.Etapas.Single(e => e.Etapa == Etapa.Recrutamento);
        var (conclusao, status, responsavel) = (recrutamento.DataConclusao, recrutamento.Status, recrutamento.UsuarioResponsavelId);

        Assert.Throws<RegraNegocioException>(() => recrutamento.Encerrar(StatusDemanda.Concluido, _c.Relogio.AgoraUtc.AddDays(9)));
        Assert.Throws<RegraNegocioException>(() => recrutamento.AtualizarStatus(StatusDemanda.EmAndamento));
        Assert.Throws<RegraNegocioException>(() => recrutamento.DefinirResponsavel(_c.OutroSesiNorte.Id));

        _c.Relogio.Avancar(TimeSpan.FromDays(5));
        demanda.IniciarExames(_c.SesiNorte, _c.Relogio);
        demanda.Finalizar(_c.SesiNorte, _c.Relogio);

        Assert.Equal((conclusao, status, responsavel), (recrutamento.DataConclusao, recrutamento.Status, recrutamento.UsuarioResponsavelId));
    }

    [Fact]
    public void Finalizada_nao_aceita_mais_nada()
    {
        var demanda = _c.Finalizada();

        Assert.Throws<RegraNegocioException>(() => demanda.Finalizar(_c.SesiNorte, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => demanda.Cancelar(_c.GestorNorte, "Tarde demais.", _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => demanda.Cancelar(_c.Admin, "Tarde demais.", _c.Relogio));
    }
}
