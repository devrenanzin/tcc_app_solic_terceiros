using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>
/// Descoberto pelo "dotnet ef" no assembly de inicialização; registra o gerador de migrations internal.
/// </summary>
internal sealed class ServicosDesignTime : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        => serviceCollection.AddSingleton<IMigrationsCodeGenerator, GeradorMigrationsInternal>();
}
