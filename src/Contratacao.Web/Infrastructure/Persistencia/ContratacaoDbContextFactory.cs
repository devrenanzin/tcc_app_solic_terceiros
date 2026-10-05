using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>
/// Usada só pelo "dotnet ef" (migrations add, database update). Lê a connection string das mesmas fontes
/// que a aplicação: user-secrets e variáveis de ambiente.
/// </summary>
internal sealed class ContratacaoDbContextFactory : IDesignTimeDbContextFactory<ContratacaoDbContext>
{
    public ContratacaoDbContext CreateDbContext(string[] args)
    {
        var configuracao = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ContratacaoDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var opcoes = new DbContextOptionsBuilder<ContratacaoDbContext>()
            .UseSqlServer(ConfiguracaoInfraestrutura.ObterConnectionString(configuracao))
            .Options;

        return new ContratacaoDbContext(opcoes);
    }
}
