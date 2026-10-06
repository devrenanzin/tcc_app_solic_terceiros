using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure;
using Contratacao.Web.Infrastructure.Carga;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Contratacao.Tests.Integracao.Apoio;

/// <summary>
/// Banco SQL Server temporário: aplica as migrations do sistema, faz a carga inicial e é apagado no fim.
/// </summary>
// public: o xUnit exige que a fixture usada em IClassFixture seja pública.
public sealed class BancoFixture : IAsyncLifetime
{
    private readonly string _connectionString = ServidorSql.ConnectionString($"Contratacao_Testes_{Guid.NewGuid():N}");

    /// <summary>Gerente executivo fictício: o catálogo começa vazio e o formulário o exige.</summary>
    internal static readonly Guid GerenteExecutivoId = Guid.Parse("00000000-0000-0000-0000-0000000000e1");

    /// <summary>OS da carga inicial usadas nos formulários dos testes: 01 (Norte) e 11 (Sudeste).</summary>
    internal static readonly Guid OsNorteId = IdsFixos.OrdemServico(1);
    internal static readonly Guid OsSudesteId = IdsFixos.OrdemServico(11);

    /// <summary>Pasta temporária dos anexos gravados pelos testes, apagada no fim.</summary>
    internal string PastaAnexos { get; } = Path.Combine(Path.GetTempPath(), $"contratacao-anexos-{Guid.NewGuid():N}");

    // 01/09/2026 08:45 em Brasília.
    internal RelogioFixo Relogio { get; } = new(new DateTime(2026, 9, 1, 11, 45, 0, DateTimeKind.Utc));

    internal static OpcoesAdminInicial Admin => new()
    {
        Nome = "Admin de Testes",
        Email = "  Admin.Testes@UCL.br ",
        Senha = "senha-de-teste-123",
    };

    internal string ConnectionString => _connectionString;

    internal ContratacaoDbContext NovoContexto()
        => new(new DbContextOptionsBuilder<ContratacaoDbContext>().UseSqlServer(_connectionString).Options);

    /// <summary>Os mesmos serviços da aplicação (Application e Infrastructure), ligados a este banco e ao relógio fixo.</summary>
    internal ServiceProvider CriarServicos()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Contratacao"] = _connectionString,
                ["Anexos:Pasta"] = PastaAnexos,
            })
            .Build();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = "Testes", ContentRootPath = PastaAnexos });
        servicos.AdicionarAplicacao();
        servicos.AdicionarInfraestrutura(configuracao);
        servicos.AddSingleton<IRelogio>(Relogio);
        return servicos.BuildServiceProvider();
    }

    /// <summary>Executa um caso de uso num escopo novo, como numa requisição.</summary>
    internal async Task<TResultado> ExecutarAsync<TServico, TResultado>(Func<TServico, Task<TResultado>> acao)
        where TServico : notnull
    {
        await using var servicos = CriarServicos();
        await using var escopo = servicos.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<TServico>());
    }

    internal Task ExecutarAsync<TServico>(Func<TServico, Task> acao)
        where TServico : notnull
        => ExecutarAsync<TServico, bool>(async servico =>
        {
            await acao(servico);
            return true;
        });

    /// <summary>Ator montado como numa requisição real: perfil e contratos lidos do banco.</summary>
    internal async Task<Ator> AtorAsync(Guid usuarioId)
        => await ExecutarAsync<ObterAtor, Ator?>(o => o.ExecutarAsync(usuarioId, "10.0.0.9", CancellationToken.None))
            ?? throw new InvalidOperationException("Usuário inexistente ou desativado.");

    internal async Task<Ator> AdminAsync()
    {
        await using var contexto = NovoContexto();
        var id = await contexto.Usuarios.Where(u => u.Perfil == Perfil.Admin).Select(u => u.Id).SingleAsync();
        return await AtorAsync(id);
    }

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

        // Os contratos do Gestor ficam em GestorContrato (usados, por exemplo, para saber quem recebe os avisos).
        if (perfil == Perfil.Gestor)
        {
            foreach (var contrato in contratos)
            {
                await contexto.Database.ExecuteSqlAsync($"INSERT INTO GestorContrato (GestorId, ContratoId) VALUES ({id}, {contrato})");
            }
        }

        return new Ator(id, perfil, true, contratos.ToHashSet(), "10.0.0.1");
    }

    internal static async Task<Demanda> CarregarDemandaAsync(ContratacaoDbContext contexto, Guid id)
        => await new RepositorioDemandas(contexto).ObterAsync(id, CancellationToken.None)
            ?? throw new InvalidOperationException("Demanda não encontrada.");

    /// <summary>Formulário completo com os catálogos reais da carga: corredor Norte, primeiro item QQP e RISCO 01.</summary>
    internal async Task<DadosSolicitacao> DadosAsync(Guid? corredorId = null)
    {
        await using var contexto = NovoContexto();
        var item = await contexto.ItensQqp.OrderBy(i => i.Codigo).Select(i => i.Id).FirstAsync();
        var rac = await contexto.Racs.OrderBy(r => r.Codigo).Select(r => r.Id).FirstAsync();
        var corredor = corredorId ?? IdsFixos.CorredorNorte;
        var contrato = await contexto.Corredores.Where(c => c.Id == corredor).Select(c => c.ContratoId).SingleAsync();

        return new DadosSolicitacao
        {
            TipoDemandaId = IdsFixos.TipoNovaContratacao,
            GerenteExecutivoId = GerenteExecutivoId,
            LocalidadeVaga = "Vitória",
            CorredorId = corredor,
            ModeloTrabalhoId = IdsFixos.ModeloPresencial,
            QuantidadeSolicitada = 2,
            DescricaoAtividades = "Apoio à manutenção preventiva.",
            ItemQqpId = item,
            Notebook = true,
            Racs = new HashSet<Guid> { rac },
            OrdemServicoId = contrato == IdsFixos.ContratoNorte ? OsNorteId : OsSudesteId,
            ColetorCusto = "CC-1234",
            ResponsavelEfetivoNome = "Pessoa Responsável",
            ResponsavelEfetivoEmail = "responsavel@ucl.br",
            FiscalEfetivoNome = "Pessoa Fiscal",
            FiscalEfetivoEmail = "fiscal@ucl.br",
        };
    }

    /// <summary>Corredor, contrato, preço e equipamentos atuais para o formulário.</summary>
    internal async Task<ReferenciasSolicitacao> ReferenciasAsync(DadosSolicitacao dados)
    {
        await using var contexto = NovoContexto();
        var catalogos = new CatalogosDemanda(contexto);
        var preco = await catalogos.PrecoQqpAsync(dados.ItemQqpId, CancellationToken.None);
        var equipamentos = await catalogos.EquipamentosAsync(CancellationToken.None);
        return (await catalogos.ReferenciasAsync(dados, preco!, equipamentos, CancellationToken.None))!;
    }

    public async ValueTask InitializeAsync()
    {
        await using var contexto = NovoContexto();
        await contexto.Database.MigrateAsync();
        await NovaCarga(contexto).ExecutarAsync(CancellationToken.None);
        await contexto.Database.ExecuteSqlAsync(
            $"INSERT INTO GerenteExecutivo (Id, Nome, Ativo) VALUES ({GerenteExecutivoId}, {"Gerência Fictícia de Testes"}, 1)");
        await contexto.Database.ExecuteSqlAsync(
            $"INSERT INTO GerenteExecutivoCorredor (GerenteExecutivoId, CorredorId) SELECT {GerenteExecutivoId}, Id FROM Corredor");

    }

    public async ValueTask DisposeAsync()
    {
        await using var contexto = NovoContexto();
        await contexto.Database.EnsureDeletedAsync();
        if (Directory.Exists(PastaAnexos))
        {
            Directory.Delete(PastaAnexos, recursive: true);
        }
    }
}
