using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>Correção com histórico de alterações (seção 12–15) e ações oferecidas a cada ator.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class AlteracoesTests
{
    private readonly Cenario _c = new();

    [Fact]
    public void Correcao_registra_so_os_campos_que_mudaram_com_usuario_perfil_e_ip()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Localidade e RACs.", _c.Relogio);
        _c.Relogio.Avancar(TimeSpan.FromHours(1));

        demanda.Corrigir(_c.Solicitante, Cenario.Dados() with
        {
            LocalidadeVaga = "Serra",
            Racs = new HashSet<Guid> { Cenario.Rac02 },
        }, Cenario.Referencias(Cenario.CorredorNorte), _c.Relogio);

        Assert.Equal(["LocalidadeVaga", "Racs"], demanda.Alteracoes.Select(a => a.Campo).Order());
        var localidade = demanda.Alteracoes.Single(a => a.Campo == "LocalidadeVaga");
        Assert.Equal(("Vitória", "Serra"), (localidade.ValorAnterior, localidade.NovoValor));
        Assert.Equal(_c.Solicitante.Id, localidade.UsuarioId);
        Assert.Equal(Perfil.Solicitante, localidade.PerfilUsuario);
        Assert.Equal("10.0.0.1", localidade.EnderecoIp);
        Assert.Equal(_c.Relogio.AgoraUtc, localidade.DataHora);

        Assert.Equal(Cenario.Rac02, Assert.Single(demanda.Racs).RacId);
        Assert.Equal(Cenario.Rac02, Assert.Single(demanda.Dados.Racs));
    }

    [Fact]
    public void Correcao_sem_mudanca_nao_gera_alteracao()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Confira os dados.", _c.Relogio);

        _c.Corrigir(demanda);

        Assert.Empty(demanda.Alteracoes);
        Assert.Equal(StatusDemanda.EmAnalise, demanda.Status);
    }

    [Fact]
    public void Troca_de_corredor_registra_corredor_e_contrato()
    {
        var demanda = _c.Aprovada();
        demanda.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Solicitante, "Corredor errado.", _c.Relogio);

        _c.Corrigir(demanda, paraSudeste: true);

        var contrato = demanda.Alteracoes.Single(a => a.Campo == nameof(Demanda.ContratoId));
        Assert.Equal((Cenario.ContratoNorte.Id.ToString(), Cenario.ContratoSudeste.Id.ToString()), (contrato.ValorAnterior, contrato.NovoValor));
        Assert.Contains(demanda.Alteracoes, a => a.Campo == nameof(DadosSolicitacao.CorredorId));
    }

    [Fact]
    public void Correcao_recusada_nao_muda_nada()
    {
        var demanda = _c.Enviada();
        demanda.DevolverPeloGestor(_c.GestorNorte, "Motivo.", _c.Relogio);

        Assert.Throws<Contratacao.Web.Domain.Comum.RegraNegocioException>(() => demanda.Corrigir(
            _c.Solicitante, Cenario.Dados() with { LocalidadeVaga = "" }, Cenario.Referencias(Cenario.CorredorNorte), _c.Relogio));

        Assert.Equal("Vitória", demanda.LocalidadeVaga);
        Assert.Empty(demanda.Alteracoes);
        Assert.NotNull(demanda.CorrecaoPendente);
    }

    [Fact]
    public void Acoes_disponiveis_seguem_perfil_contrato_e_situacao()
    {
        var enviada = _c.Enviada();
        Assert.Equal([AcaoDemanda.Aprovar, AcaoDemanda.DevolverPeloGestor, AcaoDemanda.Cancelar], enviada.AcoesDisponiveis(_c.GestorNorte).Order());
        Assert.Equal([AcaoDemanda.Aprovar, AcaoDemanda.DevolverPeloGestor, AcaoDemanda.Cancelar], enviada.AcoesDisponiveis(_c.GestorDosDois).Order());
        Assert.Empty(enviada.AcoesDisponiveis(_c.GestorSudeste));
        Assert.Empty(enviada.AcoesDisponiveis(_c.GestorInativo));
        Assert.Empty(enviada.AcoesDisponiveis(_c.SesiNorte));
        Assert.Empty(enviada.AcoesDisponiveis(_c.Solicitante));
        Assert.Empty(enviada.AcoesDisponiveis(_c.Admin));

        var aprovada = _c.Aprovada();
        Assert.Equal([AcaoDemanda.Aceitar, AcaoDemanda.DevolverPeloSesi], aprovada.AcoesDisponiveis(_c.SesiNorte).Order());
        Assert.Empty(aprovada.AcoesDisponiveis(_c.SesiSudeste));
        Assert.Equal([AcaoDemanda.Cancelar], aprovada.AcoesDisponiveis(_c.GestorNorte));

        aprovada.DevolverPeloSesi(_c.SesiNorte, TipoInconsistencia.Contratual, "Coletor.", _c.Relogio);
        Assert.Equal([AcaoDemanda.Corrigir], aprovada.AcoesDisponiveis(_c.Solicitante));
        Assert.Empty(aprovada.AcoesDisponiveis(_c.OutroSolicitante));
        Assert.Empty(aprovada.AcoesDisponiveis(_c.SesiNorte));
    }

    [Fact]
    public void Cada_perfil_ve_so_as_demandas_do_seu_escopo()
    {
        var demanda = _c.Enviada();
        bool Ve(Ator ator) => FiltroVisibilidade.Para(ator).Inclui(demanda.UsuarioSolicitanteId, demanda.ContratoId);

        Assert.True(Ve(_c.Admin));
        Assert.True(Ve(_c.GestorNorte));
        Assert.True(Ve(_c.GestorSudeste)); // todo Gestor vê todas (Cliente)
        Assert.True(Ve(_c.Solicitante));
        Assert.False(Ve(_c.OutroSolicitante));
        Assert.True(Ve(_c.SesiNorte));
        Assert.False(Ve(_c.SesiSudeste)); // critério de aceite 10
        Assert.False(Ve(_c.GestorInativo));
        Assert.False(Ve(_c.Solicitante with { Ativo = false }));
    }
}
