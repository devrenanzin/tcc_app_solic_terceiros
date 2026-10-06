using Contratacao.Web.Application.Notificacoes;

namespace Contratacao.Web.Web;

/// <summary>
/// Confere os prazos de hora em hora e gera os avisos de "perto de vencer" e "vencido" (seção 29), uma vez cada
/// por demanda. A regra fica em <see cref="VerificarPrazos"/>; aqui só o agendamento.
/// </summary>
internal sealed class VerificacaoPeriodicaPrazos(IServiceScopeFactory escopos, ILogger<VerificacaoPeriodicaPrazos> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        try
        {
            await Task.Delay(EsperaInicial, parada);
            using var relogio = new PeriodicTimer(Intervalo);
            do
            {
                await VerificarAsync(parada);
            }
            while (await relogio.WaitForNextTickAsync(parada));
        }
        catch (OperationCanceledException) when (parada.IsCancellationRequested)
        {
            // Aplicação encerrando.
        }
    }

    private async Task VerificarAsync(CancellationToken parada)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var avisos = await escopo.ServiceProvider.GetRequiredService<VerificarPrazos>().ExecutarAsync(parada);
            if (avisos > 0)
            {
                log.LogInformation("Avisos de prazo gerados para {Quantidade} demanda(s).", avisos);
            }
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            // Um erro aqui não derruba a aplicação; a próxima verificação tenta de novo.
            log.LogError(erro, "Falha ao verificar os prazos das demandas.");
        }
    }
}
