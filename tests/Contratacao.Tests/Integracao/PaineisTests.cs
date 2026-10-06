using System.Net;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application.Auditoria;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;

namespace Contratacao.Tests.Integracao;

/// <summary>Painéis, tela de acompanhamento e auditoria (Etapa 7), com SQL Server real.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class PaineisTests(PaineisTests.Cenario cenario) : IClassFixture<PaineisTests.Cenario>
{
    private readonly BancoFixture banco = cenario.Banco;

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    private Ator _solicitante => cenario.Solicitante;
    private Ator _gestorNorte => cenario.GestorNorte;
    private Ator _gestorSudeste => cenario.GestorSudeste;
    private Ator _sesiNorte => cenario.SesiNorte;
    private (Guid EmAnalise, Guid Recrutamento, Guid Cancelada, Guid Finalizada) _ids => cenario.Ids;

    [Fact]
    public async Task Acompanhamento_abre_so_com_as_em_andamento_e_o_filtro_mostra_as_outras()
    {
        var padrao = await FiltrarAsync(_solicitante, new FiltroDemandas());
        Assert.Equal(new[] { _ids.EmAnalise, _ids.Recrutamento }.Order(), padrao.Order());

        Assert.Equal([_ids.Finalizada], await FiltrarAsync(_solicitante, new FiltroDemandas { Situacao = Situacao.Concluidas }));
        Assert.Equal([_ids.Cancelada], await FiltrarAsync(_solicitante, new FiltroDemandas { Situacao = Situacao.Canceladas }));
        Assert.Equal(4, (await FiltrarAsync(_solicitante, new FiltroDemandas { Situacao = Situacao.Todas })).Count);
        Assert.Equal([_ids.Recrutamento], await FiltrarAsync(_solicitante, new FiltroDemandas { ResponsavelSesiId = _sesiNorte.Id, Etapa = Etapa.Recrutamento }));
    }

    [Fact]
    public async Task Paineis_contam_os_quadros_de_cada_perfil()
    {
        var gestorNorte = await PainelAsync(_gestorNorte);
        Assert.Equal(1, gestorNorte[Quadro.AguardandoValidacao]);
        Assert.Equal(1, gestorNorte[Quadro.EmProcessoSesi]);

        // Só "Aguardando validação" se limita aos contratos do Gestor (Cliente).
        var gestorSudeste = await PainelAsync(_gestorSudeste);
        Assert.Equal(0, gestorSudeste[Quadro.AguardandoValidacao]);
        Assert.Equal(1, gestorSudeste[Quadro.EmProcessoSesi]);

        var sesi = await PainelAsync(_sesiNorte);
        Assert.Equal((1, 1, 0), (sesi[Quadro.Recrutamento], sesi[Quadro.Finalizadas], sesi[Quadro.AguardandoAceite]));

        var solicitante = await PainelAsync(_solicitante);
        Assert.Equal((4, 2, 1, 1), (solicitante[Quadro.MinhasDemandas], solicitante[Quadro.EmAndamento], solicitante[Quadro.Finalizadas], solicitante[Quadro.Canceladas]));
    }

    [Fact]
    public async Task Pela_tela_o_padrao_e_em_andamento_e_a_situacao_troca_a_lista()
    {
        await using var admin = new Navegador(banco);
        await admin.EntrarAsync(BancoFixture.Admin.Email!.Trim().ToLowerInvariant(), BancoFixture.Admin.Senha!);

        var padrao = await admin.HtmlAsync("/Demandas");
        Assert.Contains(_ids.EmAnalise.ToString(), padrao, StringComparison.Ordinal);
        Assert.DoesNotContain(_ids.Finalizada.ToString(), padrao, StringComparison.Ordinal);
        Assert.DoesNotContain(_ids.Cancelada.ToString(), padrao, StringComparison.Ordinal);

        var canceladas = await admin.HtmlAsync("/Demandas?Situacao=Canceladas");
        Assert.Contains(_ids.Cancelada.ToString(), canceladas, StringComparison.Ordinal);
        Assert.DoesNotContain(_ids.EmAnalise.ToString(), canceladas, StringComparison.Ordinal);

        var atrasadas = await admin.HtmlAsync("/Demandas?Quadro=Atrasadas");
        Assert.Contains("Quadro do painel: <strong>Atrasadas</strong>", atrasadas, StringComparison.Ordinal);

        var inicio = await admin.HtmlAsync("/");
        Assert.Contains("/Demandas?Quadro=ProximasDoVencimento", inicio, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Auditoria_so_para_o_admin_com_filtro_e_registros_do_sistema()
    {
        var admin = await banco.AdminAsync();
        var pagina = await banco.ExecutarAsync<ConsultarAuditoria, PaginaAuditoria>(c => c.ExecutarAsync(
            admin, new FiltroAuditoria("Demanda", "Cancelamento", null, null, null, 1), Cancelamento));
        Assert.Contains(pagina.Registros, r => r.EntidadeId == _ids.Cancelada && r.Justificativa == "Vaga suspensa.");
        Assert.Contains("Anexo", pagina.Entidades);

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<ConsultarAuditoria, PaginaAuditoria>(c => c.ExecutarAsync(
            _gestorNorte, new FiltroAuditoria(null, null, null, null, null, 1), Cancelamento)));

        await using var navegador = new Navegador(banco);
        await navegador.EntrarAsync(BancoFixture.Admin.Email!.Trim().ToLowerInvariant(), BancoFixture.Admin.Senha!);
        var tela = await navegador.HtmlAsync("/Admin/Auditoria?Entidade=Demanda");
        Assert.Contains("Vaga suspensa.", tela, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Solicitante_nao_abre_a_auditoria()
    {
        await using var navegador = new Navegador(banco);
        using var cadastro = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Solicitante de Testes"), ("Entrada.Email", $"sol.{Guid.NewGuid():N}@ucl.br"),
            ("Entrada.Senha", "senha-segura-1"), ("Entrada.ConfirmacaoSenha", "senha-segura-1"));

        using var negado = await navegador.AbrirAsync("/Admin/Auditoria");
        Assert.Equal(HttpStatusCode.Redirect, negado.StatusCode);
        Assert.StartsWith("http://localhost/AcessoNegado", negado.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    private async Task<List<Guid>> FiltrarAsync(Ator ator, FiltroDemandas filtro)
        => [.. (await banco.ExecutarAsync<ConsultarDemandas, IReadOnlyList<ResumoDemanda>>(c => c.FiltrarAsync(ator, filtro, Cancelamento)))
            .Select(r => r.Id)];

    private async Task<Dictionary<Quadro, int>> PainelAsync(Ator ator)
        => (await banco.ExecutarAsync<ConsultarDemandas, IReadOnlyList<(Quadro Quadro, int Quantidade)>>(c => c.PainelAsync(ator, Cancelamento)))
            .ToDictionary(p => p.Quadro, p => p.Quantidade);

    /// <summary>Banco próprio com quatro demandas do contrato Norte, uma em cada situação, criadas uma vez para a classe.</summary>
    // public: o xUnit exige que a fixture usada em IClassFixture seja pública.
    public sealed class Cenario : IAsyncLifetime
    {
        internal BancoFixture Banco { get; } = new();
        internal Ator Solicitante { get; private set; } = null!;
        internal Ator GestorNorte { get; private set; } = null!;
        internal Ator GestorSudeste { get; private set; } = null!;
        internal Ator SesiNorte { get; private set; } = null!;
        internal (Guid EmAnalise, Guid Recrutamento, Guid Cancelada, Guid Finalizada) Ids { get; private set; }

        public async ValueTask InitializeAsync()
        {
            await Banco.InitializeAsync();
            Solicitante = await Banco.InserirUsuarioAsync(Perfil.Solicitante);
            GestorNorte = await Banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoNorte);
            GestorSudeste = await Banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoSudeste);
            SesiNorte = await Banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoNorte);

            var emAnalise = await EnviarAsync();
            var recrutamento = await AceitaAsync();
            var cancelada = await AceitaAsync();
            await Banco.ExecutarAsync<CancelarDemanda>(c => c.ExecutarAsync(GestorNorte, cancelada, "Vaga suspensa.", CancellationToken.None));
            var finalizada = await AceitaAsync();
            await Banco.ExecutarAsync<RegistrarVaga>(r => r.ExecutarAsync(SesiNorte, finalizada, "https://vagas.exemplo.ucl.br/1", CancellationToken.None));
            await Banco.ExecutarAsync<IniciarEntrevistas>(i => i.ExecutarAsync(SesiNorte, finalizada, CancellationToken.None));
            await Banco.ExecutarAsync<IniciarExames>(i => i.ExecutarAsync(SesiNorte, finalizada, CancellationToken.None));
            await Banco.ExecutarAsync<FinalizarContratacao>(f => f.ExecutarAsync(SesiNorte, finalizada, CancellationToken.None));
            Ids = (emAnalise, recrutamento, cancelada, finalizada);
        }

        public ValueTask DisposeAsync() => Banco.DisposeAsync();

        private async Task<Guid> EnviarAsync()
        {
            var dados = await Banco.DadosAsync();
            return (await Banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(Solicitante, dados, SolicitacaoTests.Vp2(), [], CancellationToken.None))).Id;
        }

        private async Task<Guid> AceitaAsync()
        {
            var id = await EnviarAsync();
            await Banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(GestorNorte, id, CancellationToken.None));
            await Banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(SesiNorte, id, CancellationToken.None));
            return id;
        }
    }
}
