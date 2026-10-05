using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Contratacao.Tests.Integracao;

/// <summary>Prova da Etapa 0: Razor Pages e endpoints mínimos funcionam com tipos internal.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class InterfaceInternalTests
{
    [Fact]
    public async Task Pagina_com_PageModel_internal_responde()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/", TestContext.Current.CancellationToken);
        var html = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("data-prova=\"pagemodel-internal\"", html);
        Assert.Contains("Estrutura inicial em funcionamento.", html);
    }

    [Fact]
    public async Task Endpoint_minimo_com_handler_internal_responde()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetFromJsonAsync<Saude>("/saude", TestContext.Current.CancellationToken);

        Assert.Equal("ok", resposta?.Status);
    }

    private sealed record Saude(string Status);
}
