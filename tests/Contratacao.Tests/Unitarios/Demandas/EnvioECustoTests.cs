using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Envio (UC02), contrato pelo corredor (RN13) e custo total mensal (RN12).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class EnvioECustoTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Custo_e_quantidade_vezes_preco_mais_equipamentos_por_pessoa()
    {
        var porPessoa = CustoDemanda.EquipamentosPorPessoa(true, true, true, Cenario.Equipamentos);

        Assert.Equal(616.92m, porPessoa);
        Assert.Equal(3 * (10_000.00m + 616.92m), CustoDemanda.Total(3, 10_000.00m, porPessoa));
        Assert.Equal(0m, CustoDemanda.EquipamentosPorPessoa(false, false, false, Cenario.Equipamentos));
        Assert.Equal(118.64m, CustoDemanda.EquipamentosPorPessoa(false, false, true, Cenario.Equipamentos));
    }

    [Fact]
    public void Envio_grava_campos_contrato_do_corredor_e_custo_com_os_valores_vigentes()
    {
        var demanda = _c.Enviada();

        Assert.Equal(Cenario.ContratoNorte.Id, demanda.ContratoId);
        Assert.Equal(Cenario.ContratadaSesi, demanda.ContratadaId);
        Assert.Equal(Cenario.CorredorNorte.Id, demanda.CorredorId);
        Assert.Equal("Vitória", demanda.LocalidadeVaga);
        Assert.Equal(Cenario.Rac01, Assert.Single(demanda.Racs).RacId);

        // 2 vagas × (10.000,00 + notebook 444,35)
        Assert.Equal(5_000.00m, demanda.PisoSalarialQqp);
        Assert.Equal(10_000.00m, demanda.PrecoUnitarioQqp);
        Assert.Equal(444.35m, demanda.ValorEquipamentosPorPessoa);
        Assert.Equal(20_888.70m, demanda.CustoTotal);
        Assert.Empty(demanda.Alteracoes);
    }

    [Fact]
    public void Sem_de_acordo_vp2_a_demanda_nao_e_enviada()
    {
        var erro = Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados(), Cenario.Referencias(Cenario.CorredorNorte), false, _c.Relogio));

        Assert.Contains("VP-2", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Envio_recusa_formulario_incompleto()
        => Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados() with { LocalidadeVaga = "" }, Cenario.Referencias(Cenario.CorredorNorte), true, _c.Relogio));

    [Fact]
    public void Contrato_vem_do_corredor_e_nao_pode_ser_outro()
    {
        var corredorNorteNoContratoErrado = new ReferenciasSolicitacao(
            Cenario.CorredorNorte, Cenario.ContratoSudeste, Cenario.OsSudeste, Cenario.Gerente, Cenario.ItemAnalista, Cenario.Equipamentos);

        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados(), corredorNorteNoContratoErrado, true, _c.Relogio));
    }

    [Fact]
    public void Os_precisa_ser_do_contrato_do_corredor_e_estar_ativa()
    {
        // A OS pertence a um contrato (Cliente): OS do Sudeste num corredor do Norte é recusada.
        var osDeOutroContrato = Cenario.Referencias(Cenario.CorredorNorte) with { Os = Cenario.OsSudeste };
        var erro = Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(Cenario.Numero, _c.Solicitante,
            Cenario.Dados() with { OrdemServicoId = Cenario.OsSudeste.Id }, osDeOutroContrato, true, _c.Relogio));
        Assert.Contains("contrato do corredor", erro.Message, StringComparison.Ordinal);

        var inativa = OrdemServico.Criar(Guid.NewGuid(), Cenario.ContratoNorte.Id, "99", ativo: false);
        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(Cenario.Numero, _c.Solicitante,
            Cenario.Dados() with { OrdemServicoId = inativa.Id }, Cenario.Referencias(Cenario.CorredorNorte) with { Os = inativa }, true, _c.Relogio));
    }

    [Fact]
    public void Gerente_executivo_precisa_atender_o_corredor_e_estar_ativo()
    {
        // Cliente: cada gerente atende seus corredores; o formulário filtra e o domínio confere.
        var soDoSudeste = GerenteExecutivo.Criar(Guid.NewGuid(), "Gerência do Sudeste", Cenario.CorredorSudeste.Id);
        var erro = Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(Cenario.Numero, _c.Solicitante,
            Cenario.Dados() with { GerenteExecutivoId = soDoSudeste.Id },
            Cenario.Referencias(Cenario.CorredorNorte) with { Gerente = soDoSudeste }, true, _c.Relogio));
        Assert.Contains("não atende o corredor", erro.Message, StringComparison.Ordinal);

        var inativo = GerenteExecutivo.Criar(Guid.NewGuid(), "Gerência Antiga", Cenario.CorredorNorte.Id);
        inativo.AlterarSituacao(_c.Admin, false);
        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(Cenario.Numero, _c.Solicitante,
            Cenario.Dados() with { GerenteExecutivoId = inativo.Id },
            Cenario.Referencias(Cenario.CorredorNorte) with { Gerente = inativo }, true, _c.Relogio));
    }

    [Fact]
    public void Correcao_que_troca_o_corredor_para_outro_contrato_exige_uma_os_desse_contrato()
    {
        var demanda = _c.Aprovada();
        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Contratual, "Corredor errado.", _c.Relogio);

        Assert.Throws<RegraNegocioException>(() => demanda.Corrigir(_c.Solicitante,
            Cenario.Dados() with { CorredorId = Cenario.CorredorSudeste.Id },
            Cenario.Referencias(Cenario.CorredorSudeste) with { Os = Cenario.OsNorte }, _c.Relogio));

        _c.Corrigir(demanda, paraSudeste: true);
        Assert.Equal(Cenario.OsSudeste.Id, demanda.OrdemServicoId);
        Assert.Contains(demanda.Alteracoes, a => a.Campo == nameof(DadosSolicitacao.OrdemServicoId));
    }

    [Fact]
    public void Corredor_lido_precisa_ser_o_escolhido_e_o_preco_o_do_item_escolhido()
    {
        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados(), Cenario.Referencias(Cenario.CorredorSudeste), true, _c.Relogio));
        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados(), Cenario.Referencias(Cenario.CorredorNorte, Cenario.ItemEngenheiro), true, _c.Relogio));
    }

    [Fact]
    public void Corredor_inativo_e_recusado()
    {
        var inativo = Corredor.Criar(Guid.NewGuid(), "Antigo", Cenario.RegiaoNorte, Cenario.ContratoNorte.Id, ativo: false);

        Assert.Throws<RegraNegocioException>(() => Demanda.Enviar(
            Cenario.Numero, _c.Solicitante, Cenario.Dados() with { CorredorId = inativo.Id }, Cenario.Referencias(inativo), true, _c.Relogio));
    }

    [Fact]
    public void Correcao_que_muda_o_item_qqp_recalcula_o_custo_com_os_valores_atuais()
    {
        var demanda = DevolvidaPeloGestor();
        var equipamentosNovos = Cenario.Equipamentos with { Notebook = 500.00m };

        demanda.Corrigir(_c.Solicitante, Cenario.Dados() with { ItemQqpId = Cenario.ItemEngenheiro.ItemQqpId },
            Cenario.Referencias(Cenario.CorredorNorte, Cenario.ItemEngenheiro, equipamentosNovos), _c.Relogio);

        Assert.Equal(16_000.00m, demanda.PrecoUnitarioQqp);
        Assert.Equal(500.00m, demanda.ValorEquipamentosPorPessoa);
        Assert.Equal(2 * 16_500.00m, demanda.CustoTotal);
        var custo = Assert.Single(demanda.Alteracoes, a => a.Campo == nameof(Demanda.CustoTotal));
        Assert.Equal(("20888.70", "33000.00"), (custo.ValorAnterior, custo.NovoValor));
    }

    [Fact]
    public void Correcao_que_muda_so_a_quantidade_recalcula_o_custo_inteiro_com_os_valores_atuais()
    {
        // Cliente: mudou quantidade, equipamentos ou QQP, o custo inteiro usa os valores de hoje.
        var demanda = DevolvidaPeloGestor();
        var precoAtual = Cenario.ItemAnalista with { PrecoUnitario = 11_000.00m };

        demanda.Corrigir(_c.Solicitante, Cenario.Dados() with { QuantidadeSolicitada = 3 },
            Cenario.Referencias(Cenario.CorredorNorte, precoAtual), _c.Relogio);

        Assert.Equal(11_000.00m, demanda.PrecoUnitarioQqp);
        Assert.Equal(3 * (11_000.00m + 444.35m), demanda.CustoTotal);
    }

    [Fact]
    public void Correcao_que_muda_os_equipamentos_recalcula_o_custo()
    {
        var demanda = DevolvidaPeloGestor();

        demanda.Corrigir(_c.Solicitante, Cenario.Dados() with { Notebook = false, Celular = true },
            Cenario.Referencias(Cenario.CorredorNorte), _c.Relogio);

        Assert.Equal(118.64m, demanda.ValorEquipamentosPorPessoa);
        Assert.Equal(2 * (10_000.00m + 118.64m), demanda.CustoTotal);
    }

    [Fact]
    public void Correcao_sem_mudanca_no_custo_mantem_os_valores_do_envio()
    {
        // RN12: mudanças de preço depois do envio não alteram a demanda.
        var demanda = DevolvidaPeloGestor();
        var precoNovo = Cenario.ItemAnalista with { PrecoUnitario = 99_999.00m };

        demanda.Corrigir(_c.Solicitante, Cenario.Dados() with { LocalidadeVaga = "Serra" },
            Cenario.Referencias(Cenario.CorredorNorte, precoNovo, Cenario.Equipamentos with { Notebook = 1m }), _c.Relogio);

        Assert.Equal(10_000.00m, demanda.PrecoUnitarioQqp);
        Assert.Equal(20_888.70m, demanda.CustoTotal);
        Assert.DoesNotContain(demanda.Alteracoes, a => a.Campo == nameof(Demanda.CustoTotal));
    }

    private Demanda DevolvidaPeloGestor()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Revise o cargo.", _c.Relogio);
        return demanda;
    }
}
