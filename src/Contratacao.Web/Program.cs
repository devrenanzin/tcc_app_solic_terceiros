using Contratacao.Web.Application;
using Contratacao.Web.Infrastructure;
using Contratacao.Web.Infrastructure.Carga;
using Contratacao.Web.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AdicionarAplicacao();
builder.Services.AdicionarInfraestrutura(builder.Configuration);
builder.Services.AdicionarInterfaceWeb();

var app = builder.Build();

// dotnet run --project src/Contratacao.Web -- preparar-banco
if (args.Contains(PreparacaoBanco.Comando))
{
    await PreparacaoBanco.ExecutarAsync(app.Services);
    return;
}

app.UsarInterfaceWeb();

app.Run();

// O .NET 10 gera "public partial class Program" para os testes de integração.
// A declaração explícita mantém Program internal; os testes o acessam por InternalsVisibleTo.
internal partial class Program;
