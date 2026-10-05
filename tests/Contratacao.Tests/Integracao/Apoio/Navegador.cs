using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Contratacao.Tests.Integracao.Apoio;

/// <summary>
/// Simula o navegador: guarda os cookies, não segue redirecionamentos e envia formulários com o
/// token antifalsificação da própria página, como o usuário faria.
/// </summary>
internal sealed partial class Navegador : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _fabrica;

    internal Navegador(BancoFixture banco)
    {
        // Ambiente "Testes": não carrega os user-secrets de desenvolvimento.
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(construtor =>
        {
            construtor.UseEnvironment("Testes");
            construtor.UseSetting("ConnectionStrings:Contratacao", banco.ConnectionString);
            construtor.ConfigureServices(servicos => servicos.AddSingleton<IStartupFilter, IpDoNavegador>());
        });
        Cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>IP que o servidor de testes atribui às requisições (em memória não há conexão de rede).</summary>
    internal const string Ip = "192.0.2.10";

    internal HttpClient Cliente { get; }

    internal Task<HttpResponseMessage> AbrirAsync(string caminho) => Cliente.GetAsync(caminho, TestContext.Current.CancellationToken);

    internal async Task<string> HtmlAsync(string caminho)
    {
        using var resposta = await AbrirAsync(caminho);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Abre a página, pega o token do formulário e envia os campos.</summary>
    internal async Task<HttpResponseMessage> EnviarAsync(string caminho, params (string Campo, string Valor)[] campos)
    {
        var html = await HtmlAsync(caminho);
        var token = Token().Match(html);
        Assert.True(token.Success, $"Formulário sem token antifalsificação em {caminho}.");

        var dados = campos.Select(c => new KeyValuePair<string, string>(c.Campo, c.Valor))
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token.Groups[1].Value));
        return await Cliente.PostAsync(caminho, new FormUrlEncodedContent(dados), TestContext.Current.CancellationToken);
    }

    internal async Task EntrarAsync(string email, string senha)
    {
        using var resposta = await EnviarAsync("/Entrar", ("Entrada.Email", email), ("Entrada.Senha", senha));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location?.OriginalString);
    }

    /// <summary>Primeiro acesso de quem recebeu senha inicial: entra e troca a senha.</summary>
    internal async Task EntrarPelaPrimeiraVezAsync(string email, string senhaInicial, string novaSenha)
    {
        await EntrarAsync(email, senhaInicial);
        using var troca = await EnviarAsync("/TrocarSenha",
            ("Entrada.SenhaAtual", senhaInicial), ("Entrada.NovaSenha", novaSenha), ("Entrada.ConfirmacaoSenha", novaSenha));
        Assert.Equal(HttpStatusCode.Redirect, troca.StatusCode);
        Assert.Equal("/", troca.Headers.Location?.OriginalString);
    }

    public async ValueTask DisposeAsync()
    {
        Cliente.Dispose();
        await _fabrica.DisposeAsync();
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex Token();

    private sealed class IpDoNavegador : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> proximo) => app =>
        {
            app.Use((contexto, seguinte) =>
            {
                contexto.Connection.RemoteIpAddress = IPAddress.Parse(Ip);
                return seguinte(contexto);
            });
            proximo(app);
        };
    }
}
