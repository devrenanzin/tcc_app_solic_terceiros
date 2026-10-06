using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Notificacoes;

namespace Contratacao.Tests.Unitarios.Notificacoes;

/// <summary>Quem recebe cada aviso (seção 29; Cliente, revisão de 06/10/2026) e leitura dos avisos.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class NotificacaoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Destinatarios_seguem_a_secao_29_e_os_pedidos_do_cliente()
    {
        Assert.Equal([Destinatario.GestoresDoContrato], RegrasNotificacao.Para(TipoNotificacao.NovaDemanda));
        Assert.Equal([Destinatario.GerenciadoraDoContrato, Destinatario.Solicitante], RegrasNotificacao.Para(TipoNotificacao.DemandaAprovada));
        Assert.Equal([Destinatario.Solicitante], RegrasNotificacao.Para(TipoNotificacao.CorrecaoSolicitada));
        Assert.Equal([Destinatario.GestorDaDemanda], RegrasNotificacao.Para(TipoNotificacao.AceitaPelaGerenciadora));
        Assert.Equal([Destinatario.GestoresDoContrato, Destinatario.GerenciadoraDoContrato], RegrasNotificacao.Para(TipoNotificacao.ProximaDoVencimento));
        Assert.Equal([Destinatario.GestoresDoContrato, Destinatario.GerenciadoraDoContrato], RegrasNotificacao.Para(TipoNotificacao.PrazoVencido));
        Assert.Equal([Destinatario.Solicitante], RegrasNotificacao.Para(TipoNotificacao.ContratacaoFinalizada));
        Assert.Equal([Destinatario.Solicitante], RegrasNotificacao.Para(TipoNotificacao.DemandaCancelada));

        Assert.True(RegrasNotificacao.UmaVezPorDemanda(TipoNotificacao.PrazoVencido));
        Assert.False(RegrasNotificacao.UmaVezPorDemanda(TipoNotificacao.CorrecaoSolicitada));
    }

    [Fact]
    public void So_o_dono_le_o_aviso_e_a_primeira_leitura_fica()
    {
        var aviso = Notificacao.Criar(_c.Solicitante.Id, Guid.NewGuid(), TipoNotificacao.CorrecaoSolicitada, "Demanda devolvida.", _c.Relogio.AgoraUtc);

        Assert.Throws<RegraNegocioException>(() => aviso.MarcarLida(_c.GestorNorte, _c.Relogio.AgoraUtc));
        Assert.False(aviso.Lida);

        var primeira = _c.Relogio.AgoraUtc;
        aviso.MarcarLida(_c.Solicitante, primeira);
        aviso.MarcarLida(_c.Solicitante, primeira.AddHours(1));

        Assert.Equal(primeira, aviso.DataLeitura);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Aviso_precisa_de_mensagem(string mensagem)
        => Assert.Throws<RegraNegocioException>(() => Notificacao.Criar(Guid.NewGuid(), null, TipoNotificacao.NovaDemanda, mensagem, _c.Relogio.AgoraUtc));
}
