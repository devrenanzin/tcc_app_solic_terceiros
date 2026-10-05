using Contratacao.Web.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AdicionarInterfaceWeb();

var app = builder.Build();

app.UsarInterfaceWeb();

app.Run();

// O .NET 10 gera "public partial class Program" para os testes de integração.
// A declaração explícita mantém Program internal; os testes o acessam por InternalsVisibleTo.
internal partial class Program;
