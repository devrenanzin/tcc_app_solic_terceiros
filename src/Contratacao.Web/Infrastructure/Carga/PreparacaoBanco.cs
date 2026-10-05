using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Contratacao.Web.Infrastructure.Carga;

/// <summary>
/// Comando "preparar-banco": aplica as migrations pendentes e faz a carga inicial.
/// Uso: dotnet run --project src/Contratacao.Web -- preparar-banco
/// </summary>
internal static class PreparacaoBanco
{
    internal const string Comando = "preparar-banco";

    internal static string PastaDados => Path.Combine(AppContext.BaseDirectory, "dados");

    internal static async Task ExecutarAsync(IServiceProvider servicos, CancellationToken cancelamento = default)
    {
        await using var escopo = servicos.CreateAsyncScope();
        var provedor = escopo.ServiceProvider;
        var contexto = provedor.GetRequiredService<ContratacaoDbContext>();
        var log = provedor.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PreparacaoBanco));

        log.LogInformation("Aplicando migrations...");
        await contexto.Database.MigrateAsync(cancelamento);

        log.LogInformation("Carregando dados iniciais de {Pasta}...", PastaDados);
        var carga = new CargaInicial(
            contexto,
            provedor.GetRequiredService<IRelogio>(),
            provedor.GetRequiredService<IPasswordHasher<Usuario>>(),
            provedor.GetRequiredService<IOptions<OpcoesAdminInicial>>().Value,
            PastaDados);
        await carga.ExecutarAsync(cancelamento);

        log.LogInformation("Banco pronto.");
    }
}
