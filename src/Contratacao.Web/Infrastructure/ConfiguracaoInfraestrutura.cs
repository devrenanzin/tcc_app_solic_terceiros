using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Carga;
using Contratacao.Web.Infrastructure.Persistencia;
using Contratacao.Web.Infrastructure.Tempo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure;

internal static class ConfiguracaoInfraestrutura
{
    internal const string NomeConnectionString = "Contratacao";

    internal static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio, RelogioSistema>();
        services.AddSingleton<ICalendarioSla, CalendarioBrasilia>();
        services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.Configure<OpcoesAdminInicial>(configuracao.GetSection(OpcoesAdminInicial.Secao));

        // A connection string só é exigida quando o banco é usado, para a aplicação subir sem ela.
        services.AddDbContext<ContratacaoDbContext>(opcoes => opcoes.UseSqlServer(ObterConnectionString(configuracao)));

        return services;
    }

    internal static string ObterConnectionString(IConfiguration configuracao)
        => configuracao.GetConnectionString(NomeConnectionString)
        ?? throw new InvalidOperationException(
            $"Connection string '{NomeConnectionString}' não configurada. Use: dotnet user-secrets set " +
            $"\"ConnectionStrings:{NomeConnectionString}\" \"<connection string>\" --project src/Contratacao.Web");
}
