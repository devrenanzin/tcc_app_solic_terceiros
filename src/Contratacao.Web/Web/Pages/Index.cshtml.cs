using System.Security.Claims;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;

namespace Contratacao.Web.Web.Pages;

/// <summary>Início: o painel do perfil, com os quadros das seções 21–22, e os atalhos.</summary>
internal sealed class IndexModel(ConsultarDemandas consultar, AtorAtual atorAtual) : PaginaBase
{
    internal string Nome { get; private set; } = string.Empty;
    internal Perfil? Perfil { get; private set; }
    internal IReadOnlyList<(Quadro Quadro, int Quantidade)> Painel { get; private set; } = [];

    /// <summary>Chave do rascunho deste usuário no navegador (quadro "Rascunho" do Solicitante).</summary>
    internal string ChaveRascunho => $"contratacao.rascunho.{User.FindFirstValue(ClaimTypes.NameIdentifier)}";

    public async Task OnGetAsync()
    {
        Nome = User.Identity?.Name ?? string.Empty;
        Perfil = Enum.TryParse<Perfil>(User.FindFirstValue(ClaimTypes.Role), out var perfil) ? perfil : null;
        Painel = await consultar.PainelAsync(await atorAtual.ObterAsync(), Cancelamento);
    }
}
