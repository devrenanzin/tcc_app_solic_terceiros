using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Tests.Unitarios.Anexos;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Processo SESI (UC08–11) com SQL Server real: sequência, datas congeladas gravadas e telas.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class ProcessoSesiTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sesi_conduz_da_vaga_a_finalizacao_e_as_datas_ficam_gravadas()
    {
        var (id, sesi, outroSesi) = await AceitaAsync();

        banco.Relogio.Avancar(TimeSpan.FromDays(1));
        await banco.ExecutarAsync<RegistrarVaga>(r => r.ExecutarAsync(sesi, id, " https://vagas.exemplo.ucl.br/77 ", Cancelamento));
        banco.Relogio.Avancar(TimeSpan.FromDays(1));
        await banco.ExecutarAsync<IniciarEntrevistas>(i => i.ExecutarAsync(outroSesi, id, Cancelamento));
        var fimRecrutamento = banco.Relogio.AgoraUtc;
        banco.Relogio.Avancar(TimeSpan.FromDays(1));
        await banco.ExecutarAsync<IniciarExames>(i => i.ExecutarAsync(sesi, id, Cancelamento));
        banco.Relogio.Avancar(TimeSpan.FromDays(1));
        await banco.ExecutarAsync<FinalizarContratacao>(f => f.ExecutarAsync(sesi, id, Cancelamento));

        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        Assert.Equal((Etapa.Contratacao, StatusDemanda.Concluido), (demanda.Etapa, demanda.Status));
        Assert.Equal("https://vagas.exemplo.ucl.br/77", demanda.Vaga!.LinkExterno);
        Assert.Equal(sesi.Id, demanda.Vaga.UsuarioResponsavelId);
        var recrutamento = demanda.Etapas.Single(e => e.Etapa == Etapa.Recrutamento);
        Assert.Equal((fimRecrutamento, outroSesi.Id), (recrutamento.DataConclusao, recrutamento.UsuarioResponsavelId));
        Assert.Equal(DateTimeKind.Utc, recrutamento.DataConclusao!.Value.Kind);
        Assert.All(demanda.Etapas, e => Assert.False(e.Aberta));
        Assert.Equal(Farol.Verde, demanda.ObterFarol(banco.Relogio, new CalendarioBrasilia()));
        Assert.Equal(
            [EventoDemanda.AceitaPeloSesi, EventoDemanda.VagaAberta, EventoDemanda.EntrevistasIniciadas, EventoDemanda.ExamesIniciados, EventoDemanda.ContratacaoFinalizada],
            demanda.Historico.OrderBy(h => h.DataHora).Select(h => h.Evento).SkipWhile(e => e != EventoDemanda.AceitaPeloSesi));
    }

    [Fact]
    public async Task Sequencia_e_contrato_sao_conferidos_e_nada_muda_numa_acao_recusada()
    {
        var (id, sesi, _) = await AceitaAsync();
        var sesiSudeste = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoSudeste);

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<IniciarEntrevistas>(i => i.ExecutarAsync(sesi, id, Cancelamento)));
        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<FinalizarContratacao>(f => f.ExecutarAsync(sesi, id, Cancelamento)));
        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            banco.ExecutarAsync<RegistrarVaga>(r => r.ExecutarAsync(sesiSudeste, id, "https://vagas.exemplo.ucl.br/1", Cancelamento)));
        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            banco.ExecutarAsync<RegistrarVaga>(r => r.ExecutarAsync(sesi, id, "sem-protocolo", Cancelamento)));

        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        Assert.Equal((Etapa.Recrutamento, StatusDemanda.EmAndamento), (demanda.Etapa, demanda.Status));
        Assert.Null(demanda.Vaga);
    }

    [Fact]
    public async Task Pela_tela_o_sesi_ve_so_o_proximo_passo_e_conclui_a_contratacao()
    {
        var (id, _, _) = await AceitaAsync();
        var caminho = $"/Demandas/Detalhe/{id}";
        var (email, senha) = await CadastrarSesiComSenhaAsync();

        await using var sesi = new Navegador(banco);
        await sesi.EntrarPelaPrimeiraVezAsync(email, senha, "senha-propria-2");

        var pagina = await sesi.HtmlAsync(caminho);
        Assert.Contains("handler=RegistrarVaga", pagina, StringComparison.Ordinal);
        Assert.DoesNotContain("handler=Finalizar", pagina, StringComparison.Ordinal);

        using var vazia = await sesi.EnviarDeAsync(caminho, caminho + "?handler=RegistrarVaga", ("LinkVaga", ""));
        Assert.Contains("link http ou https", await sesi.HtmlAsync(caminho), StringComparison.Ordinal);

        foreach (var (handler, campos) in new (string, (string, string)[])[]
        {
            ("RegistrarVaga", [("LinkVaga", "https://vagas.exemplo.ucl.br/88")]),
            ("IniciarEntrevistas", []),
            ("IniciarExames", []),
            ("Finalizar", []),
        })
        {
            using var resposta = await sesi.EnviarDeAsync(caminho, $"{caminho}?handler={handler}", campos);
            Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        }

        var final = await sesi.HtmlAsync(caminho);
        Assert.Contains("https://vagas.exemplo.ucl.br/88", final, StringComparison.Ordinal);
        Assert.Contains("Contratação finalizada", final, StringComparison.Ordinal);
        Assert.DoesNotContain("Processo SESI · próximo passo", final, StringComparison.Ordinal);
        Assert.Contains("bi-lock-fill", final, StringComparison.Ordinal);
    }

    /// <summary>Demanda enviada, aprovada pelo Gestor e aceita pelo SESI do contrato Norte.</summary>
    private async Task<(Guid Id, Ator Sesi, Ator OutroSesi)> AceitaAsync()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var sesi = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);
        var outroSesi = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();

        var demanda = await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(
            solicitante, dados, SolicitacaoTests.Arquivo("vp2.pdf", RegraArquivoTests.Pdf), [], Cancelamento));
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, demanda.Id, Cancelamento));
        await banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(sesi, demanda.Id, Cancelamento));
        return (demanda.Id, sesi, outroSesi);
    }

    /// <summary>Funcionário SESI do contrato Norte com login, cadastrado por um Gestor desse contrato.</summary>
    private async Task<(string Email, string Senha)> CadastrarSesiComSenhaAsync()
    {
        const string senha = "senha-segura-1";
        var admin = await banco.AdminAsync();
        var gestor = await banco.ExecutarAsync<Contratacao.Web.Application.Usuarios.CadastrarGestor, Usuario>(c => c.ExecutarAsync(
            admin, "Gestor de Testes", $"gestor.{Guid.NewGuid():N}@ucl.br", senha, [IdsFixos.ContratoNorte], Cancelamento));
        var ator = await banco.AtorAsync(gestor.Id);
        var email = $"sesi.{Guid.NewGuid():N}@ucl.br";
        await banco.ExecutarAsync<Contratacao.Web.Application.Usuarios.CadastrarFuncionarioSesi, Usuario>(c => c.ExecutarAsync(
            ator, "SESI de Testes", email, senha, IdsFixos.ContratoNorte, Cancelamento));
        return (email, senha);
    }
}
