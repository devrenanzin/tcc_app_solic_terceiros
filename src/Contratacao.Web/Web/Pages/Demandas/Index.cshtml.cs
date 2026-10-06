using System.Security.Claims;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Web.Autenticacao;
using Microsoft.AspNetCore.Mvc;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// UC03 e tela de acompanhamento (seções 21–22): as demandas no escopo do usuário (seção 4), com os campos e
/// filtros da tela do SESI. Abre só com as em andamento; o filtro de situação mostra concluídas e canceladas (Cliente).
/// </summary>
internal sealed class IndexModel(ConsultarDemandas consultar, AtorAtual atorAtual, IRelogio relogio, ICalendarioSla calendario) : PaginaBase
{
    [BindProperty(SupportsGet = true)] public Quadro? Quadro { get; set; }
    [BindProperty(SupportsGet = true)] public Situacao Situacao { get; set; } = Situacao.EmAndamento;
    [BindProperty(SupportsGet = true)] public Etapa? Etapa { get; set; }
    [BindProperty(SupportsGet = true)] public StatusDemanda? Status { get; set; }
    [BindProperty(SupportsGet = true)] public Farol? Farol { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ContratadaId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? GestorId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? SolicitanteId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ResponsavelSesiId { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? EnviadaDe { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? EnviadaAte { get; set; }
    [BindProperty(SupportsGet = true)] public string? Numero { get; set; }

    internal IReadOnlyList<ResumoDemanda> Demandas { get; private set; } = [];
    internal Perfil? Perfil { get; private set; }
    internal DateOnly Hoje { get; private set; }

    // Opções das listas de filtro: só o que aparece nas demandas que o usuário vê.
    internal IReadOnlyList<(Guid Id, string Nome)> Contratadas { get; private set; } = [];
    internal IReadOnlyList<(Guid Id, string Nome)> Gestores { get; private set; } = [];
    internal IReadOnlyList<(Guid Id, string Nome)> Solicitantes { get; private set; } = [];
    internal IReadOnlyList<(Guid Id, string Nome)> Responsaveis { get; private set; } = [];

    internal int Visiveis { get; private set; }

    /// <summary>Algum filtro além da situação padrão está em uso.</summary>
    internal bool Filtrado => Quadro is not null || Situacao != Situacao.EmAndamento || Etapa is not null || Status is not null
        || Farol is not null || ContratadaId is not null || GestorId is not null || SolicitanteId is not null
        || ResponsavelSesiId is not null || EnviadaDe is not null || EnviadaAte is not null || !string.IsNullOrWhiteSpace(Numero);

    public async Task OnGetAsync()
    {
        Perfil = Enum.TryParse<Perfil>(User.FindFirstValue(ClaimTypes.Role), out var perfil) ? perfil : null;
        Hoje = calendario.DataLocal(relogio.AgoraUtc);

        var ator = await atorAtual.ObterAsync();
        var todas = await consultar.ListarAsync(ator, Cancelamento);
        var filtro = new FiltroDemandas
        {
            Quadro = Quadro,
            Situacao = Situacao,
            Etapa = Etapa,
            Status = Status,
            Farol = Farol,
            ContratadaId = ContratadaId,
            GestorId = GestorId,
            SolicitanteId = SolicitanteId,
            ResponsavelSesiId = ResponsavelSesiId,
            EnviadaDe = EnviadaDe,
            EnviadaAte = EnviadaAte,
            Numero = Numero,
        };

        Demandas = [.. todas.Where(r => filtro.Aceita(r, ator, calendario))];
        Visiveis = todas.Count;
        Contratadas = Opcoes(todas.Select(r => ((Guid?)r.ContratadaId, (string?)r.Contratada)));
        Gestores = Opcoes(todas.Select(r => (r.GestorId, r.Gestor)));
        Solicitantes = Opcoes(todas.Select(r => ((Guid?)r.SolicitanteId, (string?)r.Solicitante)));
        Responsaveis = Opcoes(todas.Select(r => (r.ResponsavelSesiId, r.ResponsavelSesi)));
    }

    private static IReadOnlyList<(Guid Id, string Nome)> Opcoes(IEnumerable<(Guid? Id, string? Nome)> pares)
        => [.. pares.Where(p => p is { Id: not null, Nome: not null }).Select(p => (p.Id!.Value, p.Nome!)).Distinct().OrderBy(p => p.Item2)];
}
