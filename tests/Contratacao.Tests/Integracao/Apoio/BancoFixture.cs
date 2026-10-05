using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Carga;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao.Apoio;

/// <summary>
/// Banco SQL Server temporário: aplica as migrations do sistema, faz a carga inicial e é apagado no fim.
/// </summary>
// public: o xUnit exige que a fixture usada em IClassFixture seja pública.
public sealed class BancoFixture : IAsyncLifetime
{
    private readonly string _connectionString = ServidorSql.ConnectionString($"Contratacao_Testes_{Guid.NewGuid():N}");

    // 01/09/2026 08:45 em Brasília.
    internal RelogioFixo Relogio { get; } = new(new DateTime(2026, 9, 1, 11, 45, 0, DateTimeKind.Utc));

    internal static OpcoesAdminInicial Admin => new()
    {
        Nome = "Admin de Testes",
        Email = "  Admin.Testes@UCL.br ",
        Senha = "senha-de-teste-123",
    };

    internal ContratacaoDbContext NovoContexto()
        => new(new DbContextOptionsBuilder<ContratacaoDbContext>().UseSqlServer(_connectionString).Options);

    internal CargaInicial NovaCarga(ContratacaoDbContext contexto)
        => new(contexto, Relogio, new PasswordHasher<Usuario>(), Admin, PreparacaoBanco.PastaDados);

    /// <summary>Insere um usuário fictício direto na tabela; os casos de uso de cadastro entram na Etapa 3.</summary>
    internal async Task<Ator> InserirUsuarioAsync(Perfil perfil, params Guid[] contratos)
    {
        var id = Guid.NewGuid();
        var login = $"teste.{id:N}";
        var perfilId = IdsFixos.Perfis[perfil];
        Guid? contratoSesi = perfil == Perfil.FuncionarioSesi ? contratos.Single() : null;

        await using var contexto = NovoContexto();
        await contexto.Database.ExecuteSqlAsync($"""
            INSERT INTO Usuario (Id, PerfilId, ContratoId, Nome, Email, Login, Ativo, DataCadastro, SenhaHash)
            VALUES ({id}, {perfilId}, {contratoSesi}, {"Usuário de teste"}, {login + "@ucl.br"}, {login}, 1, SYSUTCDATETIME(), {"hash"})
            """);

        return new Ator(id, perfil, true, contratos.ToHashSet(), "10.0.0.1");
    }

    internal static Task<Demanda> CarregarDemandaAsync(ContratacaoDbContext contexto, Guid id)
        => contexto.Demandas
            .Include(d => d.Etapas)
            .Include(d => d.Correcoes)
            .Include(d => d.Historico)
            .Include(d => d.Vaga)
            .SingleAsync(d => d.Id == id);

    public async ValueTask InitializeAsync()
    {
        await using var contexto = NovoContexto();
        await contexto.Database.MigrateAsync();
        await NovaCarga(contexto).ExecutarAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await using var contexto = NovoContexto();
        await contexto.Database.EnsureDeletedAsync();
    }
}
