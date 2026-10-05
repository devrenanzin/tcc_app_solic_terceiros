using Contratacao.Tests.Integracao.Prova;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Contratacao.Tests.Integracao;

/// <summary>
/// Prova da Etapa 0: migrations geradas pelo "dotnet ef" saem internal e o EF Core as encontra e aplica.
/// A migration de prova (Integracao/Prova/Migrations) foi gerada com:
/// dotnet ef migrations add ProvaInicial --project tests/Contratacao.Tests --startup-project tests/Contratacao.Tests
///   --context ProvaDbContext --output-dir Integracao/Prova/Migrations
/// </summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class MigrationsInternalTests
{
    [Fact]
    public void Gerador_troca_public_por_internal()
    {
        const string gerado = "namespace X\n{\n    public partial class Inicial : Migration\n    {\n    }\n}";

        var resultado = GeradorMigrationsInternal.TornarInternal(gerado);

        Assert.Contains("internal partial class Inicial : Migration", resultado);
        Assert.DoesNotContain("public partial class", resultado);
    }

    [Fact]
    public void Migrations_e_snapshot_gerados_nao_sao_publicos()
    {
        var tiposEf = typeof(ProvaDbContext).Assembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) || typeof(ModelSnapshot).IsAssignableFrom(t))
            .ToList();

        Assert.Equal(2, tiposEf.Count);
        Assert.All(tiposEf, t => Assert.False(t.IsVisible, $"{t.FullName} é público"));
    }

    [Fact]
    public async Task Migration_internal_e_aplicada_no_SQL_Server()
    {
        var cancelamento = TestContext.Current.CancellationToken;
        var nomeBanco = $"Contratacao_Prova_{Guid.NewGuid():N}";
        await using var contexto = ProvaDbContextFactory.Criar(nomeBanco);

        try
        {
            await contexto.Database.MigrateAsync(cancelamento);

            var aplicadas = await contexto.Database.GetAppliedMigrationsAsync(cancelamento);
            Assert.Contains(aplicadas, m => m.EndsWith("_ProvaInicial", StringComparison.Ordinal));

            var entidade = new ProvaEntidade { Id = Guid.NewGuid(), Nome = "Registro de prova" };
            contexto.Entidades.Add(entidade);
            await contexto.SaveChangesAsync(cancelamento);
            contexto.ChangeTracker.Clear();

            var lida = await contexto.Entidades.SingleAsync(e => e.Id == entidade.Id, cancelamento);
            Assert.Equal("Registro de prova", lida.Nome);
        }
        finally
        {
            await contexto.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }
}
