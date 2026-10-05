namespace Contratacao.Tests.Integracao.Apoio;

/// <summary>Servidor SQL dos testes de integração: LocalDB, ou o informado em CONTRATACAO_TESTES_SQL (sem o banco).</summary>
internal static class ServidorSql
{
    private const string Padrao = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    internal static string ConnectionString(string nomeBanco)
        => $"{Environment.GetEnvironmentVariable("CONTRATACAO_TESTES_SQL") ?? Padrao};Database={nomeBanco}";
}
