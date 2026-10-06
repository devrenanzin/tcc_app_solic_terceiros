using System.Globalization;
using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Tests.Unitarios.Anexos;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Cancelamento (UC18), operações excepcionais (UC20) e anexos na correção, com SQL Server real.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class CancelamentoEExcecoesTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Gestor_cancela_com_justificativa_e_tudo_continua_gravado()
    {
        var (id, gestor, sesi) = await EmRecrutamentoAsync();

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(sesi, id, "Gerenciadora não cancela.", Cancelamento)));
        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(gestor, id, " ", Cancelamento)));
        await banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(gestor, id, "Vaga suspensa pela área.", Cancelamento));

        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        Assert.Equal((Etapa.Recrutamento, StatusDemanda.Cancelado, "Vaga suspensa pela área."), (demanda.Etapa, demanda.Status, demanda.MotivoCancelamento));
        Assert.Equal(EventoDemanda.Cancelada, demanda.Historico.OrderBy(h => h.DataHora).Last().Evento);
        Assert.True(await contexto.Anexos.AnyAsync(a => a.DemandaId == id, Cancelamento));
        var log = await contexto.LogsAuditoria.SingleAsync(l => l.EntidadeId == id, Cancelamento);
        Assert.Equal(("Cancelamento", "Vaga suspensa pela área.", gestor.Id), (log.Acao, log.Justificativa, log.UsuarioId));
    }

    [Fact]
    public async Task Admin_cancela_volta_etapa_e_altera_datas_com_auditoria()
    {
        var (id, _, _) = await EmRecrutamentoAsync();
        var admin = await banco.AdminAsync();
        banco.Relogio.Avancar(TimeSpan.FromDays(1));

        await banco.ExecutarAsync<ForcarTransicao>(f => f.ExecutarAsync(admin, id, Etapa.ExamesMedicos, "Etapas feitas fora do sistema.", Cancelamento));
        await banco.ExecutarAsync<ForcarTransicao>(f => f.ExecutarAsync(admin, id, Etapa.Recrutamento, "Candidato reprovado.", Cancelamento));

        Demanda demanda;
        await using (var contexto = banco.NovoContexto())
        {
            demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        }

        var inicioSla = demanda.Sla!.InicioUtc;
        await banco.ExecutarAsync<AlterarDataExcepcional>(a => a.ExecutarAsync(
            admin, id, DataAlteravel.InicioSla, null, inicioSla.AddMinutes(30), "Aprovação registrada com atraso.", Cancelamento));
        var aceite = demanda.Etapas.Single(e => e.Etapa == Etapa.ValidacaoSesi);
        await banco.ExecutarAsync<AlterarDataExcepcional>(a => a.ExecutarAsync(
            admin, id, DataAlteravel.InicioEtapa, aceite.Id, inicioSla.AddMinutes(30), "Mesma correção.", Cancelamento));
        await banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(admin, id, "Encerrada por decisão da diretoria.", Cancelamento));

        await using var leitura = banco.NovoContexto();
        var final = await BancoFixture.CarregarDemandaAsync(leitura, id);
        Assert.Equal(StatusDemanda.Cancelado, final.Status);
        Assert.Equal(inicioSla.AddMinutes(30), final.Sla!.InicioUtc);
        Assert.Equal(inicioSla.AddMinutes(30), final.Etapas.Single(e => e.Id == aceite.Id).DataInicio);
        Assert.Equal(2, final.Historico.Count(h => h.Evento == EventoDemanda.TransicaoForcada));
        Assert.Contains(final.Alteracoes, a => a.Campo == "Sla.Inicio" && a.Justificativa == "Aprovação registrada com atraso.");

        var acoes = await leitura.LogsAuditoria.Where(l => l.EntidadeId == id).Select(l => l.Acao).ToListAsync(Cancelamento);
        Assert.Equal(
            ["OperacaoExcepcional.AlteracaoData", "OperacaoExcepcional.AlteracaoData", "OperacaoExcepcional.Cancelamento",
             "OperacaoExcepcional.Transicao", "OperacaoExcepcional.Transicao"],
            acoes.Order());
        Assert.True(await leitura.LogsAuditoria.Where(l => l.EntidadeId == id).AllAsync(l => l.Justificativa != null && l.UsuarioId == admin.Id, Cancelamento));
    }

    [Fact]
    public async Task Solicitante_anexa_novo_vp2_na_correcao_e_fora_dela_nao()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();
        var id = (await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento))).Id;
        var novoVp2 = (SolicitacaoTests.Arquivo("vp2-assinado.pdf", RegraArquivoTests.Pdf), CategoriaAnexo.DeAcordoVP2);

        // Fora da correção, nada é anexado nem gravado no disco.
        await banco.ExecutarAsync<DevolverDemandaPeloGestor>(d => d.ExecutarAsync(gestor, id, "Assine o VP-2.", Cancelamento));
        await banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(solicitante, id, dados, [novoVp2], Cancelamento));
        var arquivosAntes = Directory.GetFiles(banco.PastaAnexos, "*", SearchOption.AllDirectories).Length;
        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(solicitante, id, dados, [novoVp2], Cancelamento)));
        Assert.Equal(arquivosAntes, Directory.GetFiles(banco.PastaAnexos, "*", SearchOption.AllDirectories).Length);

        await using var contexto = banco.NovoContexto();
        var anexos = await contexto.Anexos.Where(a => a.DemandaId == id).OrderBy(a => a.DataUpload).ThenBy(a => a.NomeArquivo).ToListAsync(Cancelamento);
        Assert.Equal(2, anexos.Count(a => a.Categoria == CategoriaAnexo.DeAcordoVP2)); // o primeiro VP-2 continua
        Assert.Equal(Etapa.ValidacaoGestor, anexos.Single(a => a.NomeArquivo == "vp2-assinado.pdf").Etapa);
    }

    [Fact]
    public async Task Pela_tela_gestor_cancela_e_admin_altera_data_no_horario_de_brasilia()
    {
        var (id, _, _) = await EmRecrutamentoAsync();
        var caminho = $"/Demandas/Detalhe/{id}";
        var (gestorEmail, senha) = await GestorComSenhaAsync();

        await using var admin = new Navegador(banco);
        await admin.EntrarAsync(BancoFixture.Admin.Email!.Trim().ToLowerInvariant(), BancoFixture.Admin.Senha!);
        var pagina = await admin.HtmlAsync(caminho);
        Assert.Contains("Operações excepcionais", pagina, StringComparison.Ordinal);

        Demanda demanda;
        await using (var contexto = banco.NovoContexto())
        {
            demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        }

        var vaga = demanda.Etapas.Single(e => e.Etapa == Etapa.ValidacaoSesi);
        var novaUtc = DateTime.SpecifyKind(vaga.DataConclusao!.Value.AddMinutes(-10), DateTimeKind.Utc);
        novaUtc = novaUtc.AddTicks(-(novaUtc.Ticks % TimeSpan.TicksPerMinute));
        var digitada = TimeZoneInfo.ConvertTimeFromUtc(novaUtc, Brasilia).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        using var alteracao = await admin.EnviarDeAsync(caminho, $"{caminho}?handler=AlterarData",
            ("DataAlvo", $"ConclusaoEtapa:{vaga.Id}"), ("NovaData", digitada), ("Justificativa", "Aceite registrado depois."));
        Assert.Equal(HttpStatusCode.Redirect, alteracao.StatusCode);

        await using var gestor = new Navegador(banco);
        await gestor.EntrarPelaPrimeiraVezAsync(gestorEmail, senha, "senha-propria-2");
        Assert.Contains("handler=Cancelar", await gestor.HtmlAsync(caminho), StringComparison.Ordinal);
        using var cancelamento = await gestor.EnviarDeAsync(caminho, $"{caminho}?handler=Cancelar", ("Justificativa", "Área desistiu da vaga."));
        Assert.Equal(HttpStatusCode.Redirect, cancelamento.StatusCode);

        var final = await gestor.HtmlAsync(caminho);
        Assert.Contains("Demanda cancelada", final, StringComparison.Ordinal);
        Assert.Contains("Área desistiu da vaga.", final, StringComparison.Ordinal);
        Assert.Contains("Aceite registrado depois.", final, StringComparison.Ordinal);

        await using var leitura = banco.NovoContexto();
        var gravada = await BancoFixture.CarregarDemandaAsync(leitura, id);
        Assert.Equal(novaUtc, gravada.Etapas.Single(e => e.Id == vaga.Id).DataConclusao);
    }

    /// <summary>Demanda do contrato Norte aceita pela Gerenciadora (em Recrutamento), com os atores usados.</summary>
    private async Task<(Guid Id, Ator Gestor, Ator Sesi)> EmRecrutamentoAsync()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestor = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
        var sesi = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();

        var id = (await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento))).Id;
        banco.Relogio.Avancar(TimeSpan.FromHours(1));
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));
        banco.Relogio.Avancar(TimeSpan.FromHours(1));
        await banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(sesi, id, Cancelamento));
        banco.Relogio.Avancar(TimeSpan.FromHours(1));
        return (id, gestor, sesi);
    }

    private async Task<(string Email, string Senha)> GestorComSenhaAsync()
    {
        const string senha = "senha-segura-1";
        var admin = await banco.AdminAsync();
        var email = $"gestor.{Guid.NewGuid():N}@ucl.br";
        await banco.ExecutarAsync<Contratacao.Web.Application.Usuarios.CadastrarGestor, Usuario>(c => c.ExecutarAsync(
            admin, "Gestor de Testes", email, senha, [IdsFixos.ContratoNorte], Cancelamento));
        return (email, senha);
    }
}
