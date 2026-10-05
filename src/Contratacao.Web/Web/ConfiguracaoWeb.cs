using System.Text.Encodings.Web;
using System.Text.Unicode;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.WebEncoders;

namespace Contratacao.Web.Web;

internal static class ConfiguracaoWeb
{
    // As páginas ficam dentro da camada Web, e não na pasta /Pages padrão.
    internal const string PastaPaginas = "/Web/Pages";

    internal static IServiceCollection AdicionarInterfaceWeb(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AtorAtual>();

        // Sem isso o Razor grava "á" como "&#xE1;"; o texto em português sai legível no HTML.
        services.Configure<WebEncoderOptions>(opcoes => opcoes.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opcoes =>
            {
                opcoes.LoginPath = "/Entrar";
                opcoes.LogoutPath = "/Sair";
                opcoes.AccessDeniedPath = "/AcessoNegado";
                opcoes.Cookie.Name = "contratacao.sessao";
                opcoes.Cookie.HttpOnly = true;
                opcoes.Cookie.SameSite = SameSiteMode.Lax;
                opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
                opcoes.SlidingExpiration = true;
                opcoes.Events.OnValidatePrincipal = Sessao.ValidarAsync;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Admin, p => p.RequireRole(nameof(Perfil.Admin)))
            .AddPolicy(Politicas.Gestor, p => p.RequireRole(nameof(Perfil.Gestor)))
            .AddPolicy(Politicas.AdminOuGestor, p => p.RequireRole(nameof(Perfil.Admin), nameof(Perfil.Gestor)));

        services.AddRazorPages(opcoes =>
        {
            opcoes.RootDirectory = PastaPaginas;

            // Tudo exige login, exceto entrar, cadastrar-se e a página de acesso negado.
            opcoes.Conventions.AuthorizeFolder("/");
            opcoes.Conventions.AllowAnonymousToPage("/Entrar");
            opcoes.Conventions.AllowAnonymousToPage("/Cadastro");
            opcoes.Conventions.AllowAnonymousToPage("/AcessoNegado");

            opcoes.Conventions.AuthorizeFolder("/Admin", Politicas.Admin);
            opcoes.Conventions.AuthorizeFolder("/Equipe", Politicas.Gestor);
            opcoes.Conventions.AuthorizeFolder("/Solicitantes", Politicas.AdminOuGestor);
        });

        return services;
    }

    internal static WebApplication UsarInterfaceWeb(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages().WithStaticAssets();
        app.MapGet("/saude", EndpointSaude.Obter);

        return app;
    }
}
