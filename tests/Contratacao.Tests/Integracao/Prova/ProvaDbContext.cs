using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// O "dotnet ef" usa o gerador de migrations internal do projeto web ao gerar migrations neste projeto.
[assembly: DesignTimeServicesReference(
    "Contratacao.Web.Infrastructure.Persistencia.ServicosDesignTime, Contratacao.Web")]

namespace Contratacao.Tests.Integracao.Prova;

/// <summary>
/// Modelo descartável, só para provar que DbContext, entidades e migrations funcionam como internal.
/// Fica no projeto de testes porque a Etapa 0 não cria migrations no projeto web.
/// </summary>
internal sealed class ProvaDbContext(DbContextOptions<ProvaDbContext> options) : DbContext(options)
{
    internal DbSet<ProvaEntidade> Entidades => Set<ProvaEntidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProvaEntidade>(e =>
        {
            e.ToTable("ProvaEntidade");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });
    }
}

internal sealed class ProvaEntidade
{
    internal Guid Id { get; init; }
    internal string Nome { get; init; } = string.Empty;
}

/// <summary>Usado pelo "dotnet ef migrations add" para criar o contexto em tempo de design.</summary>
internal sealed class ProvaDbContextFactory : IDesignTimeDbContextFactory<ProvaDbContext>
{
    public ProvaDbContext CreateDbContext(string[] args) => Criar("Contratacao_Prova_Design");

    internal static ProvaDbContext Criar(string nomeBanco)
    {
        var options = new DbContextOptionsBuilder<ProvaDbContext>()
            .UseSqlServer(Apoio.ServidorSql.ConnectionString(nomeBanco))
            .Options;
        return new ProvaDbContext(options);
    }
}
