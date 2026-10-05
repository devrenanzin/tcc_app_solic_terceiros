namespace Contratacao.Web.Web;

internal static class ConfiguracaoWeb
{
    // As páginas ficam dentro da camada Web, e não na pasta /Pages padrão.
    internal const string PastaPaginas = "/Web/Pages";

    internal static IServiceCollection AdicionarInterfaceWeb(this IServiceCollection services)
    {
        services.AddRazorPages(opcoes => opcoes.RootDirectory = PastaPaginas);
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
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages().WithStaticAssets();
        app.MapGet("/saude", EndpointSaude.Obter);

        return app;
    }
}
