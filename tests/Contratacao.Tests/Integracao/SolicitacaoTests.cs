using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Tests.Unitarios.Anexos;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Contratacao.Tests.Integracao;

/// <summary>
/// Solicitação e validação (Etapa 4) pelos casos de uso, com SQL Server real: envio com anexos, devoluções,
/// correções e visibilidade por perfil.
/// </summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class SolicitacaoTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Envio_cria_a_demanda_com_numero_custo_anexos_e_auditoria()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync();

        var demanda = await EnviarAsync(solicitante, dados, Vp2(), [Arquivo("foto.png", RegraArquivoTests.Png)]);

        Assert.True(NumeroDemanda.EhValido(demanda.Numero));
        Assert.StartsWith("2026-", demanda.Numero, StringComparison.Ordinal);

        await using var contexto = banco.NovoContexto();
        var gravada = await BancoFixture.CarregarDemandaAsync(contexto, demanda.Id);
        var item = await contexto.ItensQqp.SingleAsync(i => i.Id == dados.ItemQqpId, Cancelamento);
        Assert.Equal(IdsFixos.ContratoNorte, gravada.ContratoId);
        Assert.Equal(item.PrecoUnitario, gravada.PrecoUnitarioQqp);
        Assert.Equal(2 * (item.PrecoUnitario + 444.35m), gravada.CustoTotal);
        Assert.Single(gravada.Racs);

        var anexos = await contexto.Anexos.Where(a => a.DemandaId == demanda.Id).ToListAsync(Cancelamento);
        Assert.Equal([CategoriaAnexo.Geral, CategoriaAnexo.DeAcordoVP2], anexos.Select(a => a.Categoria).Order());
        Assert.All(anexos, a => Assert.Equal(Etapa.Solicitacao, a.Etapa));
        var vp2 = anexos.Single(a => a.Categoria == CategoriaAnexo.DeAcordoVP2);
        Assert.Equal(("de-acordo.pdf", "application/pdf"), (vp2.NomeArquivo, vp2.TipoArquivo));

        // O nome no disco é gerado pelo sistema, nunca o original.
        Assert.DoesNotContain("de-acordo", vp2.Identificador, StringComparison.Ordinal);
        Assert.Equal(RegraArquivoTests.Pdf, await File.ReadAllBytesAsync(Path.Combine(banco.PastaAnexos, vp2.Identificador), Cancelamento));

        var uploads = await contexto.LogsAuditoria.Where(l => l.Entidade == "Anexo" && l.UsuarioId == solicitante.Id).ToListAsync(Cancelamento);
        Assert.Equal(2, uploads.Count);
        Assert.All(uploads, l => Assert.Equal("10.0.0.1", l.EnderecoIp));
    }

    [Fact]
    public async Task Sem_vp2_ou_com_arquivo_invalido_nada_e_gravado()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync();

        await Assert.ThrowsAsync<RegraNegocioException>(() => EnviarAsync(solicitante, dados, null, []));
        await Assert.ThrowsAsync<RegraNegocioException>(() => EnviarAsync(solicitante, dados, Arquivo("de-acordo.pdf", [0x4D, 0x5A]), []));

        await using var contexto = banco.NovoContexto();
        Assert.False(await contexto.Demandas.AnyAsync(d => d.UsuarioSolicitanteId == solicitante.Id, Cancelamento));
    }

    [Fact]
    public async Task Envio_recusa_gerente_executivo_inexistente_e_rac_desconhecida()
    {
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync() with
        {
            GerenteExecutivoId = Guid.NewGuid(),
            Racs = new HashSet<Guid> { Guid.NewGuid() },
        };

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => EnviarAsync(solicitante, dados, Vp2(), []));

        Assert.Contains("gerente executivo", erro.Message, StringComparison.Ordinal);
        Assert.Contains("RAC", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Devolucao_contratual_do_sesi_passa_de_novo_pelo_gestor_sem_reiniciar_o_sla()
    {
        // Critério de aceite 1.
        var (solicitante, gestor, sesi) = await AtoresAsync(IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();
        var id = (await EnviarAsync(solicitante, dados, Vp2(), [])).Id;

        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));
        var sla = (await DemandaAsync(id)).Sla!;
        banco.Relogio.Avancar(TimeSpan.FromDays(2));
        await banco.ExecutarAsync<DevolverDemandaPeloSesi>(d => d.ExecutarAsync(sesi, id, TipoInconsistencia.Contratual, "Coletor errado.", Cancelamento));
        await banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(solicitante, id, dados with { ColetorCusto = "CC-9999" }, Cancelamento));

        var corrigida = await DemandaAsync(id);
        Assert.Equal((Etapa.ValidacaoGestor, StatusDemanda.EmAnalise), (corrigida.Etapa, corrigida.Status));
        Assert.Equal("CC-9999", corrigida.ColetorCusto);

        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));
        var aprovada = await DemandaAsync(id);
        Assert.Equal((Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel), (aprovada.Etapa, aprovada.Status));
        Assert.Equal((sla.InicioUtc, sla.DataLimite), (aprovada.Sla!.InicioUtc, aprovada.Sla.DataLimite));
    }

    [Fact]
    public async Task Devolucao_do_sesi_por_erro_do_solicitante_volta_direto_ao_sesi_e_ele_aceita()
    {
        // Critério de aceite 2.
        var (solicitante, gestor, sesi) = await AtoresAsync(IdsFixos.ContratoNorte);
        var dados = await banco.DadosAsync();
        var id = (await EnviarAsync(solicitante, dados, Vp2(), [])).Id;

        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));
        await banco.ExecutarAsync<DevolverDemandaPeloSesi>(d => d.ExecutarAsync(sesi, id, TipoInconsistencia.Solicitante, "Descrição vaga.", Cancelamento));
        await banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(
            solicitante, id, dados with { DescricaoAtividades = "Inspeção de correias transportadoras." }, Cancelamento));

        var corrigida = await DemandaAsync(id);
        Assert.Equal((Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel), (corrigida.Etapa, corrigida.Status));

        await banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(sesi, id, Cancelamento));
        var aceita = await DemandaAsync(id);
        Assert.Equal((Etapa.Recrutamento, sesi.Id), (aceita.Etapa, aceita.ResponsavelSesiId));
    }

    [Fact]
    public async Task Troca_de_corredor_que_muda_o_contrato_volta_ao_gestor_do_novo_contrato_e_recalcula_o_custo()
    {
        // Critérios de aceite 12 e 13.
        var (solicitante, gestorNorte, sesiNorte) = await AtoresAsync(IdsFixos.ContratoNorte);
        var gestorSudeste = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoSudeste);
        var dados = await banco.DadosAsync();
        var id = (await EnviarAsync(solicitante, dados, Vp2(), [])).Id;
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestorNorte, id, Cancelamento));
        await banco.ExecutarAsync<DevolverDemandaPeloSesi>(d => d.ExecutarAsync(sesiNorte, id, TipoInconsistencia.Solicitante, "Corredor errado.", Cancelamento));

        Guid outroItem;
        await using (var contexto = banco.NovoContexto())
        {
            outroItem = await contexto.ItensQqp.Where(i => i.Id != dados.ItemQqpId).OrderByDescending(i => i.Codigo).Select(i => i.Id).FirstAsync(Cancelamento);
        }

        await banco.ExecutarAsync<CorrigirDemanda>(c => c.ExecutarAsync(
            solicitante, id, dados with { CorredorId = IdsFixos.CorredorSul, ItemQqpId = outroItem }, Cancelamento));

        var corrigida = await DemandaAsync(id);
        Assert.Equal(IdsFixos.ContratoSudeste, corrigida.ContratoId);
        Assert.Equal((Etapa.ValidacaoGestor, StatusDemanda.EmAnalise), (corrigida.Etapa, corrigida.Status));
        Assert.Contains(corrigida.Alteracoes, a => a.Campo == nameof(Demanda.CustoTotal));

        await using (var contexto = banco.NovoContexto())
        {
            var item = await contexto.ItensQqp.SingleAsync(i => i.Id == outroItem, Cancelamento);
            Assert.Equal(2 * (item.PrecoUnitario + 444.35m), corrigida.CustoTotal);
        }

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestorNorte, id, Cancelamento)));
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestorSudeste, id, Cancelamento));
        Assert.Equal(gestorSudeste.Id, (await DemandaAsync(id)).GestorId);
    }

    [Fact]
    public async Task Gestor_de_outro_contrato_ve_mas_nao_valida_nem_devolve()
    {
        // Critério de aceite 4.
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var gestorSudeste = await banco.InserirUsuarioAsync(Perfil.Gestor, IdsFixos.ContratoSudeste);
        var id = (await EnviarAsync(solicitante, await banco.DadosAsync(), Vp2(), [])).Id;

        var detalhe = await banco.ExecutarAsync<ConsultarDemandas, DetalheDemanda?>(c => c.DetalharAsync(gestorSudeste, id, Cancelamento));
        Assert.NotNull(detalhe);
        Assert.Empty(detalhe.Acoes);

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestorSudeste, id, Cancelamento)));
        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            banco.ExecutarAsync<DevolverDemandaPeloGestor>(d => d.ExecutarAsync(gestorSudeste, id, "Motivo.", Cancelamento)));
    }

    [Fact]
    public async Task Sesi_de_outro_contrato_nao_ve_nem_trata_e_solicitante_so_ve_as_suas()
    {
        // Critério de aceite 10.
        var (solicitante, gestor, _) = await AtoresAsync(IdsFixos.ContratoNorte);
        var sesiSudeste = await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, IdsFixos.ContratoSudeste);
        var outroSolicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var id = (await EnviarAsync(solicitante, await banco.DadosAsync(), Vp2(), [])).Id;
        await banco.ExecutarAsync<AprovarDemanda>(a => a.ExecutarAsync(gestor, id, Cancelamento));

        Assert.Null(await banco.ExecutarAsync<ConsultarDemandas, DetalheDemanda?>(c => c.DetalharAsync(sesiSudeste, id, Cancelamento)));
        Assert.Null(await banco.ExecutarAsync<ConsultarDemandas, DetalheDemanda?>(c => c.DetalharAsync(outroSolicitante, id, Cancelamento)));
        Assert.DoesNotContain(await ListarAsync(sesiSudeste), r => r.Id == id);
        Assert.DoesNotContain(await ListarAsync(outroSolicitante), r => r.Id == id);
        Assert.Contains(await ListarAsync(solicitante), r => r.Id == id);
        Assert.Contains(await ListarAsync(gestor), r => r.Id == id);
        Assert.All(await ListarAsync(solicitante), r => Assert.Equal(solicitante.Id, r.SolicitanteId));

        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<AceitarDemanda>(a => a.ExecutarAsync(sesiSudeste, id, Cancelamento)));
    }

    [Fact]
    public async Task Duas_aprovacoes_simultaneas_recusam_a_segunda_com_mensagem()
    {
        var (solicitante, gestor, _) = await AtoresAsync(IdsFixos.ContratoNorte);
        var id = (await EnviarAsync(solicitante, await banco.DadosAsync(), Vp2(), [])).Id;

        await using var servicos = banco.CriarServicos();
        await using var primeira = servicos.CreateAsyncScope();
        await using var segunda = servicos.CreateAsyncScope();
        var demandasPrimeira = primeira.ServiceProvider.GetRequiredService<IDemandas>();
        var demandasSegunda = segunda.ServiceProvider.GetRequiredService<IDemandas>();
        var naPrimeiraTela = (await demandasPrimeira.ObterAsync(id, Cancelamento))!;
        var naSegundaTela = (await demandasSegunda.ObterAsync(id, Cancelamento))!;

        naPrimeiraTela.Aprovar(gestor, 45, banco.Relogio, new CalendarioBrasilia());
        await primeira.ServiceProvider.GetRequiredService<Contratacao.Web.Application.IUnidadeDeTrabalho>().SalvarAsync(Cancelamento);

        naSegundaTela.DevolverPeloGestor(gestor, "Motivo.", banco.Relogio);
        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => segunda.ServiceProvider.GetRequiredService<Contratacao.Web.Application.IUnidadeDeTrabalho>().SalvarAsync(Cancelamento));
        Assert.Contains("Recarregue", erro.Message, StringComparison.Ordinal);
    }

    internal static ArquivoRecebido Arquivo(string nome, byte[] conteudo) => new(nome, conteudo.Length, () => new MemoryStream(conteudo));

    internal static ArquivoRecebido Vp2() => Arquivo("de-acordo.pdf", RegraArquivoTests.Pdf);

    private Task<Demanda> EnviarAsync(Ator solicitante, DadosSolicitacao dados, ArquivoRecebido? vp2, IReadOnlyList<ArquivoRecebido> outros)
        => banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, vp2, outros, Cancelamento));

    private Task<IReadOnlyList<ResumoDemanda>> ListarAsync(Ator ator)
        => banco.ExecutarAsync<ConsultarDemandas, IReadOnlyList<ResumoDemanda>>(c => c.ListarAsync(ator, Cancelamento));

    private async Task<Demanda> DemandaAsync(Guid id)
    {
        await using var contexto = banco.NovoContexto();
        return await BancoFixture.CarregarDemandaAsync(contexto, id);
    }

    private async Task<(Ator Solicitante, Ator Gestor, Ator Sesi)> AtoresAsync(Guid contrato)
        => (await banco.InserirUsuarioAsync(Perfil.Solicitante),
            await banco.InserirUsuarioAsync(Perfil.Gestor, contrato),
            await banco.InserirUsuarioAsync(Perfil.FuncionarioSesi, contrato));
}
