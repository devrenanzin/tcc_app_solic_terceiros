using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>
/// O gerador padrão do EF Core escreve "public partial class" nas migrations.
/// Este gerador troca por "internal", cumprindo a regra de tipos internal (seção 27).
/// O snapshot e o arquivo .Designer já saem sem modificador (internal por padrão).
/// </summary>
internal sealed class GeradorMigrationsInternal(
    MigrationsCodeGeneratorDependencies dependencies,
    CSharpMigrationsGeneratorDependencies csharpDependencies)
    : CSharpMigrationsGenerator(dependencies, csharpDependencies)
{
    private const string Publica = "public partial class ";
    private const string Interna = "internal partial class ";

    public override string GenerateMigration(
        string? migrationNamespace,
        string migrationName,
        IReadOnlyList<MigrationOperation> upOperations,
        IReadOnlyList<MigrationOperation> downOperations)
        => TornarInternal(base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations));

    public override string GenerateMetadata(
        string? migrationNamespace,
        Type contextType,
        string migrationName,
        string migrationId,
        IModel targetModel)
        => TornarInternal(base.GenerateMetadata(migrationNamespace, contextType, migrationName, migrationId, targetModel));

    public override string GenerateSnapshot(
        string? modelSnapshotNamespace,
        Type contextType,
        string modelSnapshotName,
        IModel model)
        => TornarInternal(base.GenerateSnapshot(modelSnapshotNamespace, contextType, modelSnapshotName, model));

    internal static string TornarInternal(string codigo) => codigo.Replace(Publica, Interna, StringComparison.Ordinal);
}
