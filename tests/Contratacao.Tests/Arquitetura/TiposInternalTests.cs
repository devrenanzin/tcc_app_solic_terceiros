namespace Contratacao.Tests.Arquitetura;

/// <summary>Regra de classes não públicas (seção 27): nenhum tipo do projeto web é visível fora do assembly.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class TiposInternalTests
{
    // Exceções públicas exigidas pelo framework. Cada uma precisa de justificativa aqui e no código.
    private static readonly HashSet<string> ExcecoesPublicas = [];

    [Fact]
    public void Nenhum_tipo_do_projeto_web_e_publico()
    {
        var publicos = Camadas.AssemblyWeb.GetTypes()
            .Where(t => t.IsVisible)
            .Select(t => t.FullName!)
            .Where(nome => !ExcecoesPublicas.Contains(nome))
            .ToList();

        Assert.Empty(publicos);
    }
}
