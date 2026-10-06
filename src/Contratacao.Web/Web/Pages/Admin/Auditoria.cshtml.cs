using Contratacao.Web.Application.Auditoria;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Admin;

/// <summary>Consultar auditoria completa (seção 19), só o Admin: o log geral, do mais recente ao mais antigo.</summary>
internal sealed class AuditoriaModel(ConsultarAuditoria consultar, AtorAtual atorAtual) : PaginaBase
{
    [BindProperty(SupportsGet = true)] public string? Entidade { get; set; }
    [BindProperty(SupportsGet = true)] public string? Acao { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? De { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Ate { get; set; }
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;

    internal PaginaAuditoria Resultado { get; private set; } = new([], 0, 1, ConsultarAuditoria.TamanhoPagina, []);

    internal int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Resultado.Total / (double)Resultado.TamanhoPagina));

    public async Task OnGetAsync()
    {
        // O período é digitado em datas de Brasília; o log guarda UTC.
        DateTime? de = De is { } d ? Formatacao.ParaUtc(d.ToDateTime(TimeOnly.MinValue)) : null;
        DateTime? ate = Ate is { } a ? Formatacao.ParaUtc(a.AddDays(1).ToDateTime(TimeOnly.MinValue)) : null;

        Resultado = await consultar.ExecutarAsync(
            await atorAtual.ObterAsync(), new FiltroAuditoria(Entidade, Acao, null, de, ate, Pagina), Cancelamento);
    }

    internal string Link(int pagina)
        => QueryString.Create(new Dictionary<string, string?>
        {
            ["Entidade"] = Entidade,
            ["Acao"] = Acao,
            ["De"] = De?.ToString("yyyy-MM-dd"),
            ["Ate"] = Ate?.ToString("yyyy-MM-dd"),
            ["Pagina"] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }.Where(p => !string.IsNullOrEmpty(p.Value))).ToUriComponent();
}
