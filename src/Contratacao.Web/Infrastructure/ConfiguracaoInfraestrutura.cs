using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Infrastructure.Tempo;

namespace Contratacao.Web.Infrastructure;

internal static class ConfiguracaoInfraestrutura
{
    internal static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio, RelogioSistema>();
        services.AddSingleton<ICalendarioSla, CalendarioBrasilia>();
        return services;
    }
}
