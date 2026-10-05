using NetArchTest.Rules;

namespace Contratacao.Tests.Arquitetura;

/// <summary>
/// Regra de dependência (seção 27, RNF11): Domain não depende de ninguém, Application só de Domain,
/// Infrastructure não depende de Web.
/// </summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class RegraDependenciaTests
{
    private static readonly string[] Frameworks = ["Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore"];

    [Fact]
    public void Domain_nao_depende_de_outras_camadas_nem_de_frameworks()
        => VerificarSemDependencia(Camadas.Domain, [Camadas.Application, Camadas.Infrastructure, Camadas.Web, .. Frameworks]);

    [Fact]
    public void Application_depende_apenas_de_Domain()
        => VerificarSemDependencia(Camadas.Application, [Camadas.Infrastructure, Camadas.Web, .. Frameworks]);

    [Fact]
    public void Infrastructure_nao_depende_de_Web()
        => VerificarSemDependencia(Camadas.Infrastructure, [Camadas.Web]);

    [Fact]
    public void Todo_tipo_do_projeto_esta_em_uma_das_quatro_camadas()
    {
        var foraDasCamadas = Camadas.AssemblyWeb.GetTypes()
            .Where(t => !EhGeradoPeloCompilador(t))
            .Where(t => t != typeof(Program))
            .Where(t => !Camadas.Todas.Any(c => t.Namespace == c || t.Namespace?.StartsWith(c + ".", StringComparison.Ordinal) == true))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(foraDasCamadas);
    }

    private static void VerificarSemDependencia(string camada, string[] proibidas)
    {
        var resultado = Types.InAssembly(Camadas.AssemblyWeb)
            .That().ResideInNamespace(camada)
            .ShouldNot().HaveDependencyOnAny(proibidas)
            .GetResult();

        var violacoes = resultado.FailingTypes?.Select(t => t.FullName) ?? [];
        Assert.True(resultado.IsSuccessful, $"{camada} viola a regra de dependência: {string.Join(", ", violacoes)}");
    }

    // Tipos gerados pelo compilador e pelo Razor (AspNetCoreGeneratedDocument) não pertencem a uma camada.
    private static bool EhGeradoPeloCompilador(Type tipo)
        => tipo.FullName is null
        || tipo.FullName.Contains('<')
        || tipo.Namespace is null
        || tipo.Namespace.StartsWith("AspNetCoreGeneratedDocument", StringComparison.Ordinal)
        || tipo.Namespace.StartsWith("Microsoft.", StringComparison.Ordinal)
        || tipo.Namespace.StartsWith("System.", StringComparison.Ordinal);
}
