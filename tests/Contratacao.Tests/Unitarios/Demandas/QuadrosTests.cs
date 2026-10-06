using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Quadros dos painéis e filtros da tela de acompanhamento (seções 21–22).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class QuadrosTests
{
    private readonly Cenario _c = new();

    private static PosicaoDemanda Em(Etapa etapa, StatusDemanda status, Farol farol = Farol.Verde, Contratacao.Web.Domain.Contratos.Contrato? contrato = null)
        => new(etapa, status, farol, (contrato ?? Cenario.ContratoNorte).Id);

    [Fact]
    public void Cada_perfil_tem_os_quadros_da_secao_21()
    {
        Assert.Equal(5, Quadros.DoPerfil(Perfil.Solicitante).Count);
        Assert.Equal(
            [Quadro.AguardandoValidacao, Quadro.CorrecoesPendentes, Quadro.Aprovadas, Quadro.EmProcessoSesi, Quadro.ProximasDoVencimento, Quadro.Atrasadas],
            Quadros.DoPerfil(Perfil.Gestor));
        Assert.Equal(7, Quadros.DoPerfil(Perfil.FuncionarioSesi).Count);
    }

    [Fact]
    public void Aguardando_validacao_e_so_dos_contratos_do_gestor_e_os_demais_quadros_contam_todas()
    {
        var doSudeste = Em(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise, contrato: Cenario.ContratoSudeste);

        Assert.True(Quadros.Inclui(Quadro.AguardandoValidacao, Em(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise), _c.GestorNorte));
        Assert.False(Quadros.Inclui(Quadro.AguardandoValidacao, doSudeste, _c.GestorNorte));
        Assert.True(Quadros.Inclui(Quadro.AguardandoValidacao, doSudeste, _c.GestorDosDois));

        // Cliente: só "Aguardando validação" se limita aos contratos do Gestor.
        Assert.True(Quadros.Inclui(Quadro.Atrasadas, Em(Etapa.Entrevistas, StatusDemanda.EmAndamento, Farol.Vermelho, Cenario.ContratoSudeste), _c.GestorNorte));
        Assert.True(Quadros.Inclui(Quadro.CorrecoesPendentes, Em(Etapa.ValidacaoGestor, StatusDemanda.AguardandoCorrecao, contrato: Cenario.ContratoSudeste), _c.GestorNorte));
    }

    [Fact]
    public void Aprovadas_sao_as_que_aguardam_o_sesi_e_em_processo_sao_as_aceitas()
    {
        var aguardandoSesi = Em(Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel);
        var emRecrutamento = Em(Etapa.Recrutamento, StatusDemanda.EmAndamento);

        Assert.True(Quadros.Inclui(Quadro.Aprovadas, aguardandoSesi, _c.GestorNorte));
        Assert.False(Quadros.Inclui(Quadro.EmProcessoSesi, aguardandoSesi, _c.GestorNorte));
        Assert.False(Quadros.Inclui(Quadro.Aprovadas, emRecrutamento, _c.GestorNorte));
        Assert.True(Quadros.Inclui(Quadro.EmProcessoSesi, emRecrutamento, _c.GestorNorte));
        Assert.True(Quadros.Inclui(Quadro.Recrutamento, emRecrutamento, _c.SesiNorte));
        Assert.False(Quadros.Inclui(Quadro.Entrevistas, emRecrutamento, _c.SesiNorte));
    }

    [Theory]
    [InlineData("Amarelo", true, false)]
    [InlineData("Laranja", true, false)]
    [InlineData("Vermelho", false, true)]
    [InlineData("Verde", false, false)]
    public void Proximas_do_vencimento_e_atrasadas_seguem_o_farol(string farol, bool proxima, bool atrasada)
    {
        var demanda = Em(Etapa.Entrevistas, StatusDemanda.EmAndamento, Enum.Parse<Farol>(farol));

        Assert.Equal(proxima, Quadros.Inclui(Quadro.ProximasDoVencimento, demanda, _c.GestorNorte));
        Assert.Equal(atrasada, Quadros.Inclui(Quadro.Atrasadas, demanda, _c.GestorNorte));
    }

    [Fact]
    public void Finalizada_depois_do_prazo_nao_conta_como_atrasada()
        => Assert.False(Quadros.Inclui(Quadro.Atrasadas, Em(Etapa.Contratacao, StatusDemanda.Concluido, Farol.Vermelho), _c.GestorNorte));

    [Fact]
    public void Situacao_padrao_mostra_so_em_andamento_e_as_outras_mostram_concluidas_e_canceladas()
    {
        var ativa = Em(Etapa.ValidacaoGestor, StatusDemanda.AguardandoCorrecao);
        var concluida = Em(Etapa.Contratacao, StatusDemanda.Concluido);
        var cancelada = Em(Etapa.Recrutamento, StatusDemanda.Cancelado, Farol.Cancelado);

        Assert.Equal(Situacao.EmAndamento, new FiltroDemandas().Situacao);
        Assert.Equal([true, false, false], new[] { ativa, concluida, cancelada }.Select(d => Quadros.Inclui(Situacao.EmAndamento, d)));
        Assert.Equal([false, true, false], new[] { ativa, concluida, cancelada }.Select(d => Quadros.Inclui(Situacao.Concluidas, d)));
        Assert.Equal([false, false, true], new[] { ativa, concluida, cancelada }.Select(d => Quadros.Inclui(Situacao.Canceladas, d)));
        Assert.Equal([true, true, true], new[] { ativa, concluida, cancelada }.Select(d => Quadros.Inclui(Situacao.Todas, d)));
    }

    [Fact]
    public void Filtro_combina_campos_e_periodo_pela_data_de_envio_em_brasilia()
    {
        var calendario = new CalendarioBrasilia();
        var gestor = Guid.NewGuid();
        // 02/09/2026 01:30 UTC = 01/09/2026 22:30 em Brasília.
        var resumo = Resumo(new DateTime(2026, 9, 2, 1, 30, 0, DateTimeKind.Utc), gestor);

        Assert.True(new FiltroDemandas().Aceita(resumo, _c.GestorNorte, calendario));
        Assert.True(new FiltroDemandas { EnviadaDe = new DateOnly(2026, 9, 1), EnviadaAte = new DateOnly(2026, 9, 1) }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.False(new FiltroDemandas { EnviadaDe = new DateOnly(2026, 9, 2) }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.True(new FiltroDemandas { GestorId = gestor, Etapa = Etapa.Entrevistas, Numero = "000123" }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.False(new FiltroDemandas { GestorId = Guid.NewGuid() }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.False(new FiltroDemandas { Farol = Farol.Vermelho }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.False(new FiltroDemandas { Situacao = Situacao.Concluidas }.Aceita(resumo, _c.GestorNorte, calendario));
        Assert.True(new FiltroDemandas { Quadro = Quadro.EmProcessoSesi, Situacao = Situacao.Concluidas }.Aceita(resumo, _c.GestorNorte, calendario));
    }

    private static ResumoDemanda Resumo(DateTime envioUtc, Guid gestorId)
        => new(Guid.NewGuid(), "2026-000123", Guid.NewGuid(), "Solicitante", Cenario.ContratoNorte.Id, "5900125082",
            Cenario.ContratadaSesi, "SESI", gestorId, "Gestor", Guid.NewGuid(), "SESI de testes", Etapa.Entrevistas, StatusDemanda.EmAndamento,
            envioUtc, "Vitória", "Analista", 2, 1000m, null, null) { Farol = Farol.Verde };
}
