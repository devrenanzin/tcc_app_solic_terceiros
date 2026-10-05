using System.Text.Json;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>
/// Campos do formulário como chegam da tela (seção 8.1). As regras (obrigatórios, condicionais, limites)
/// ficam em <see cref="DadosSolicitacao.Validar"/>; aqui só há a conversão.
/// </summary>
internal sealed class EntradaDemanda
{
    public string? AreaSolicitante { get; set; }
    public Guid? TipoDemandaId { get; set; }
    public Guid? GerenteExecutivoId { get; set; }
    public string? LocalidadeVaga { get; set; }
    public Guid? CorredorId { get; set; }
    public Guid? ModeloTrabalhoId { get; set; }
    public int? QuantidadeSolicitada { get; set; }
    public string? DescricaoAtividades { get; set; }
    public string? Formacao { get; set; }
    public bool Temporaria { get; set; }
    public int? PeriodoTemporarioMeses { get; set; }
    public Guid? ItemQqpId { get; set; }
    public bool Notebook { get; set; }
    public bool SegundaTela { get; set; }
    public bool Celular { get; set; }
    public bool ExigeCnh { get; set; }
    public string? CategoriaCnh { get; set; }
    public List<Guid> Racs { get; set; } = [];
    public string? ContratoOs { get; set; }
    public string? ColetorCusto { get; set; }
    public string? ResponsavelEfetivoNome { get; set; }
    public string? ResponsavelEfetivoEmail { get; set; }
    public string? FiscalEfetivoNome { get; set; }
    public string? FiscalEfetivoEmail { get; set; }
    public string? Observacoes { get; set; }

    internal DadosSolicitacao ParaDados() => new()
    {
        AreaSolicitante = AreaSolicitante,
        TipoDemandaId = TipoDemandaId ?? Guid.Empty,
        GerenteExecutivoId = GerenteExecutivoId ?? Guid.Empty,
        LocalidadeVaga = LocalidadeVaga ?? string.Empty,
        CorredorId = CorredorId ?? Guid.Empty,
        ModeloTrabalhoId = ModeloTrabalhoId ?? Guid.Empty,
        QuantidadeSolicitada = QuantidadeSolicitada ?? 0,
        DescricaoAtividades = DescricaoAtividades ?? string.Empty,
        Formacao = Formacao,
        Temporaria = Temporaria,
        PeriodoTemporarioMeses = PeriodoTemporarioMeses,
        ItemQqpId = ItemQqpId ?? Guid.Empty,
        Notebook = Notebook,
        SegundaTela = SegundaTela,
        Celular = Celular,
        ExigeCnh = ExigeCnh,
        CategoriaCnh = CategoriaCnh,
        Racs = Racs.ToHashSet(),
        ContratoOs = ContratoOs ?? string.Empty,
        ColetorCusto = ColetorCusto ?? string.Empty,
        ResponsavelEfetivoNome = ResponsavelEfetivoNome ?? string.Empty,
        ResponsavelEfetivoEmail = ResponsavelEfetivoEmail ?? string.Empty,
        FiscalEfetivoNome = FiscalEfetivoNome ?? string.Empty,
        FiscalEfetivoEmail = FiscalEfetivoEmail ?? string.Empty,
        Observacoes = Observacoes,
    };

    internal static EntradaDemanda De(DadosSolicitacao dados) => new()
    {
        AreaSolicitante = dados.AreaSolicitante,
        TipoDemandaId = dados.TipoDemandaId,
        GerenteExecutivoId = dados.GerenteExecutivoId,
        LocalidadeVaga = dados.LocalidadeVaga,
        CorredorId = dados.CorredorId,
        ModeloTrabalhoId = dados.ModeloTrabalhoId,
        QuantidadeSolicitada = dados.QuantidadeSolicitada,
        DescricaoAtividades = dados.DescricaoAtividades,
        Formacao = dados.Formacao,
        Temporaria = dados.Temporaria,
        PeriodoTemporarioMeses = dados.PeriodoTemporarioMeses,
        ItemQqpId = dados.ItemQqpId,
        Notebook = dados.Notebook,
        SegundaTela = dados.SegundaTela,
        Celular = dados.Celular,
        ExigeCnh = dados.ExigeCnh,
        CategoriaCnh = dados.CategoriaCnh,
        Racs = [.. dados.Racs],
        ContratoOs = dados.ContratoOs,
        ColetorCusto = dados.ColetorCusto,
        ResponsavelEfetivoNome = dados.ResponsavelEfetivoNome,
        ResponsavelEfetivoEmail = dados.ResponsavelEfetivoEmail,
        FiscalEfetivoNome = dados.FiscalEfetivoNome,
        FiscalEfetivoEmail = dados.FiscalEfetivoEmail,
        Observacoes = dados.Observacoes,
    };
}

/// <summary>O que o formulário (_Formulario) precisa para ser desenhado.</summary>
internal sealed record FormularioDemanda(EntradaDemanda Entrada, CatalogosFormulario Catalogos, string ItensQqpJson)
{
    internal static async Task<FormularioDemanda> CarregarAsync(ConsultarFormulario consulta, EntradaDemanda entrada, CancellationToken cancelamento)
    {
        var catalogos = await consulta.CatalogosAsync(cancelamento);
        var itens = await consulta.ItensQqpAsync(cancelamento);

        // Nomes curtos: são 1.019 itens embutidos na página para as listas em cascata.
        var json = JsonSerializer.Serialize(itens.Select(i => new
        {
            i = i.Id,
            c = i.Codigo,
            r = i.Regiao,
            f = i.Funcao,
            k = i.Classificacao,
            n = i.Nivel,
            o = i.OrdemNivel,
            h = i.CargaHoraria,
            p = i.PisoSalarial,
            u = i.PrecoUnitario,
        }));

        return new FormularioDemanda(entrada, catalogos, json);
    }
}
