using System.Globalization;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>Nomes legíveis dos campos e valores gravados no histórico de alterações (HistoricoAlteracao).</summary>
internal static class RotulosDemanda
{
    private static readonly Dictionary<string, string> Rotulos = new()
    {
        [nameof(DadosSolicitacao.AreaSolicitante)] = "Área do solicitante",
        [nameof(DadosSolicitacao.TipoDemandaId)] = "Tipo de demanda",
        [nameof(DadosSolicitacao.GerenteExecutivoId)] = "Gerente executivo",
        [nameof(DadosSolicitacao.LocalidadeVaga)] = "Localidade da vaga",
        [nameof(DadosSolicitacao.CorredorId)] = "Corredor",
        [nameof(DadosSolicitacao.ModeloTrabalhoId)] = "Modelo de trabalho",
        [nameof(DadosSolicitacao.QuantidadeSolicitada)] = "Quantidade de vagas",
        [nameof(DadosSolicitacao.DescricaoAtividades)] = "Descrição das atividades",
        [nameof(DadosSolicitacao.Formacao)] = "Formação exigida",
        [nameof(DadosSolicitacao.Temporaria)] = "Vaga temporária",
        [nameof(DadosSolicitacao.PeriodoTemporarioMeses)] = "Período temporário (meses)",
        [nameof(DadosSolicitacao.ItemQqpId)] = "Cargo QQP",
        [nameof(DadosSolicitacao.Notebook)] = "Notebook",
        [nameof(DadosSolicitacao.SegundaTela)] = "Segunda tela",
        [nameof(DadosSolicitacao.Celular)] = "Celular",
        [nameof(DadosSolicitacao.ExigeCnh)] = "Exige CNH",
        [nameof(DadosSolicitacao.CategoriaCnh)] = "Categoria da CNH",
        [nameof(DadosSolicitacao.Racs)] = "RACs",
        [nameof(DadosSolicitacao.ContratoOs)] = "OS do contrato",
        [nameof(DadosSolicitacao.ColetorCusto)] = "Coletor de custo",
        [nameof(DadosSolicitacao.ResponsavelEfetivoNome)] = "Responsável efetivo",
        [nameof(DadosSolicitacao.ResponsavelEfetivoEmail)] = "E-mail do responsável efetivo",
        [nameof(DadosSolicitacao.FiscalEfetivoNome)] = "Fiscal efetivo",
        [nameof(DadosSolicitacao.FiscalEfetivoEmail)] = "E-mail do fiscal efetivo",
        [nameof(DadosSolicitacao.Observacoes)] = "Observações",
        [nameof(Demanda.ContratoId)] = "Contrato",
        [nameof(Demanda.PisoSalarialQqp)] = "Piso salarial QQP",
        [nameof(Demanda.PrecoUnitarioQqp)] = "Preço unitário QQP",
        [nameof(Demanda.ValorEquipamentosPorPessoa)] = "Equipamentos por pessoa",
        [nameof(Demanda.CustoTotal)] = "Custo total mensal",
    };

    internal static string Rotulo(string campo) => Rotulos.GetValueOrDefault(campo, campo);

    internal static string Valor(string campo, string? valor, DescricoesDemanda nomes)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return campo == nameof(DadosSolicitacao.Racs) ? "Nenhuma" : "—";
        }

        return campo switch
        {
            nameof(DadosSolicitacao.TipoDemandaId) => Nome(nomes.TiposDemanda, valor),
            nameof(DadosSolicitacao.GerenteExecutivoId) => Nome(nomes.GerentesExecutivos, valor),
            nameof(DadosSolicitacao.CorredorId) => Nome(nomes.Corredores, valor),
            nameof(DadosSolicitacao.ModeloTrabalhoId) => Nome(nomes.ModelosTrabalho, valor),
            nameof(DadosSolicitacao.ItemQqpId) => Nome(nomes.ItensQqp, valor),
            nameof(Demanda.ContratoId) => Nome(nomes.Contratos, valor),
            nameof(DadosSolicitacao.Racs) => string.Join(", ", valor.Split(';').Select(v => Nome(nomes.Racs, v))),
            nameof(Demanda.PisoSalarialQqp) or nameof(Demanda.PrecoUnitarioQqp) or nameof(Demanda.ValorEquipamentosPorPessoa) or nameof(Demanda.CustoTotal)
                => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var reais) ? Formatacao.Reais(reais) : valor,
            _ => valor,
        };
    }

    private static string Nome(IReadOnlyDictionary<Guid, string> nomes, string valor)
        => Guid.TryParse(valor, out var id) ? DescricoesDemanda.Nome(nomes, id) : valor;
}
