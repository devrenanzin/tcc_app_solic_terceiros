using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Application.Notificacoes;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Notificacoes;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Avisos dentro do sistema (Etapa 8), com SQL Server real: quem recebe cada um e os avisos de prazo.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class NotificacoesTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Cada_passo_avisa_quem_deve_ser_avisado()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestorNorte = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var outroGestorNorte = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var gestorSudeste = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoSudeste);
        var gerenciadoraNorte = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);
        var gerenciadoraSudeste = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoSudeste);
        var dados = await banco.DadosAsync();

        var id = (await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(
            solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento))).Id;
        // Grupos: todos os Gestores ativos do contrato (outros testes da classe também criam Gestores do Norte).
        var novaDemanda = await AvisosAsync(id, TipoNotificacao.NovaDemanda);
        Assert.Contains(gestorNorte.Id, novaDemanda);
        Assert.Contains(outroGestorNorte.Id, novaDemanda);
        Assert.DoesNotContain(gestorSudeste.Id, novaDemanda);

        await banco.ExecutarAsync<DevolverDemandaPeloGestor>(d => d.ExecutarAsync(gestorNorte, id, "Ajuste a localidade.", Cancelamento));
        Assert.Equal([solicitante.Id], await AvisosAsync(id, TipoNotificacao.CorrecaoSolicitada));

        await banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(solicitante, id, dados, [], Cancelamento));
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestorNorte, id, Cancelamento));
        var aprovada = await AvisosAsync(id, TipoNotificacao.DemandaAprovada);
        Assert.Contains(solicitante.Id, aprovada);
        Assert.Contains(gerenciadoraNorte.Id, aprovada);
        Assert.DoesNotContain(gestorNorte.Id, aprovada);

        await banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(gerenciadoraNorte, id, Cancelamento));
        Assert.Equal([gestorNorte.Id], await AvisosAsync(id, TipoNotificacao.AceitaPelaGerenciadora)); // o Gestor da demanda

        await banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(outroGestorNorte, id, "Vaga suspensa.", Cancelamento));
        Assert.Equal([solicitante.Id], await AvisosAsync(id, TipoNotificacao.DemandaCancelada));

        // Ninguém do contrato Sudeste foi avisado.
        await using var contexto = banco.NovoContexto();
        Assert.False(await contexto.Notificacoes.AnyAsync(
            n => n.DemandaId == id && (n.UsuarioId == gestorSudeste.Id || n.UsuarioId == gerenciadoraSudeste.Id), Cancelamento));
    }

    [Fact]
    public async Task Avisos_de_prazo_saem_uma_vez_quando_fica_perto_de_vencer_e_uma_vez_quando_vence()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var gerenciadora = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();
        var inicio = banco.Relogio.AgoraUtc;

        try
        {
            var id = (await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento))).Id;
            await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));

            await VerificarAsync();
            Assert.Empty(await AvisosAsync(id, TipoNotificacao.ProximaDoVencimento)); // farol verde

            banco.Relogio.Avancar(TimeSpan.FromDays(37)); // 8 dias restantes: amarelo
            await VerificarAsync();
            await VerificarAsync();
            Assert.Contains(gestor.Id, await AvisosAsync(id, TipoNotificacao.ProximaDoVencimento));
            Assert.Contains(gerenciadora.Id, await AvisosAsync(id, TipoNotificacao.ProximaDoVencimento));
            Assert.Equal(1, (await AvisosAsync(id, TipoNotificacao.ProximaDoVencimento)).Count(u => u == gestor.Id));

            banco.Relogio.Avancar(TimeSpan.FromDays(10)); // vencida
            await VerificarAsync();
            await VerificarAsync();
            Assert.Equal(1, (await AvisosAsync(id, TipoNotificacao.PrazoVencido)).Count(u => u == gerenciadora.Id));
        }
        finally
        {
            banco.Relogio.Definir(inicio);
        }
    }

    [Fact]
    public async Task Sininho_mostra_os_avisos_e_abrir_marca_como_lido()
    {
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var email = $"sol.{Guid.NewGuid():N}@ucl.br";
        await using var navegador = new Navegador(banco);
        using (var cadastro = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Solicitante de Testes"), ("Entrada.Email", email),
            ("Entrada.Senha", "senha-segura-1"), ("Entrada.ConfirmacaoSenha", "senha-segura-1")))
        {
            Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        }

        Guid solicitanteId;
        await using (var contexto = banco.NovoContexto())
        {
            solicitanteId = (await contexto.Usuarios.SingleAsync(u => u.Email == email, Cancelamento)).Id;
        }

        var solicitante = await banco.AtorAsync(solicitanteId);
        var dados = await banco.DadosAsync();
        var id = (await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(
            solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento))).Id;
        await banco.ExecutarAsync<DevolverDemandaPeloGestor>(d => d.ExecutarAsync(gestor, id, "Ajuste.", Cancelamento));

        var inicio = await navegador.HtmlAsync("/");
        Assert.Contains("contador-avisos", inicio, StringComparison.Ordinal);
        var avisos = await navegador.HtmlAsync("/Notificacoes");
        Assert.Contains("devolvida para correção", avisos, StringComparison.Ordinal);

        Guid avisoId;
        await using (var contexto = banco.NovoContexto())
        {
            avisoId = (await contexto.Notificacoes.SingleAsync(n => n.UsuarioId == solicitanteId, Cancelamento)).Id;
        }

        using var abrir = await navegador.EnviarDeAsync("/Notificacoes", $"/Notificacoes?handler=Abrir&id={avisoId}");
        Assert.Equal($"/Demandas/Detalhe/{id}", abrir.Headers.Location?.OriginalString);
        Assert.DoesNotContain("contador-avisos", await navegador.HtmlAsync("/"), StringComparison.Ordinal);
    }

    private async Task VerificarAsync() => await banco.ExecutarAsync<VerificarPrazos, int>(v => v.ExecutarAsync(Cancelamento));

    private async Task<List<Guid>> AvisosAsync(Guid demandaId, TipoNotificacao tipo)
    {
        await using var contexto = banco.NovoContexto();
        return await contexto.Notificacoes.Where(n => n.DemandaId == demandaId && n.Tipo == tipo).Select(n => n.UsuarioId).ToListAsync(Cancelamento);
    }
}
