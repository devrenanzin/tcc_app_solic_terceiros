using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Número AAAA-NNNNNN (UC02, RF40): sequencial sem repetição sob envios simultâneos (critério de aceite 7).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class NumeracaoTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Envios_simultaneos_recebem_numeros_sequenciais_sem_repeticao()
    {
        const int envios = 20;
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync();

        // Um envio recusado antes não consome número.
        await Assert.ThrowsAsync<RegraNegocioException>(() => banco.ExecutarAsync<EnviarDemanda, Demanda>(
            e => e.ExecutarAsync(solicitante, dados, null, [], Cancelamento)));

        var numeros = await Task.WhenAll(Enumerable.Range(0, envios).Select(_ => banco.ExecutarAsync<EnviarDemanda, string>(
            async e => (await e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento)).Numero)));

        Assert.Equal(
            Enumerable.Range(1, envios).Select(n => NumeroDemanda.Formatar(2026, n)),
            numeros.Order(StringComparer.Ordinal));

        await using var contexto = banco.NovoContexto();
        var sequencia = await contexto.SequenciasNumeroDemanda.SingleAsync(s => s.Ano == 2026, Cancelamento);
        Assert.Equal(envios, sequencia.UltimoNumero);
    }

    [Fact]
    public async Task Numero_usa_o_ano_do_envio_no_horario_de_brasilia()
    {
        // SUPOSIÇÃO (S3): o sequencial reinicia a cada ano. 31/12/2027 23:30 em Brasília já é 2028 em UTC.
        var solicitante = await banco.InserirUsuarioAsync(Perfil.Solicitante);
        var dados = await banco.DadosAsync();
        var antes = banco.Relogio.AgoraUtc;
        banco.Relogio.Definir(new DateTime(2028, 1, 1, 2, 30, 0, DateTimeKind.Utc));

        try
        {
            var demanda = await banco.ExecutarAsync<EnviarDemanda, Demanda>(e => e.ExecutarAsync(solicitante, dados, SolicitacaoTests.Vp2(), [], Cancelamento));
            Assert.Equal("2027-000001", demanda.Numero);
        }
        finally
        {
            banco.Relogio.Definir(antes);
        }
    }
}
