using System.Security.Claims;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC03 — Demandas no escopo do usuário (seção 4): Admin e Gestor, todas; Solicitante, as suas; SESI, as do
/// seu contrato. Os painéis por perfil, com filtros, entram na Etapa 7.
/// </summary>
internal sealed class IndexModel(ConsultarDemandas consultar, AtorAtual atorAtual) : PaginaBase
{
    internal IReadOnlyList<ResumoDemanda> Demandas { get; private set; } = [];
    internal Perfil? Perfil { get; private set; }

    public async Task OnGetAsync()
    {
        Perfil = Enum.TryParse<Perfil>(User.FindFirstValue(ClaimTypes.Role), out var perfil) ? perfil : null;
        Demandas = await consultar.ListarAsync(await atorAtual.ObterAsync(), Cancelamento);
    }
}
