using System.Globalization;
using System.Text.RegularExpressions;
using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>
/// Campos do formulário da demanda (seção 8.1), preenchidos e corrigidos só pelo Solicitante.
/// O número do contrato não está aqui: vem do corredor (RN13).
/// </summary>
internal sealed partial record DadosSolicitacao
{
    internal static readonly IReadOnlyList<string> CategoriasCnh = ["A", "B", "C", "D", "E", "AB", "AC", "AD", "AE"];

    // Identificação
    internal string? AreaSolicitante { get; init; }
    internal Guid TipoDemandaId { get; init; }
    internal Guid GerenteExecutivoId { get; init; }

    // Vaga
    internal string LocalidadeVaga { get; init; } = string.Empty;
    internal Guid CorredorId { get; init; }
    internal Guid ModeloTrabalhoId { get; init; }
    internal int QuantidadeSolicitada { get; init; }
    internal string DescricaoAtividades { get; init; } = string.Empty;
    internal string? Formacao { get; init; }
    internal bool Temporaria { get; init; }

    /// <summary>Em meses (Cliente, revisão de 05/10/2026); só quando Temporaria.</summary>
    internal int? PeriodoTemporarioMeses { get; init; }

    // Cargo e preço: o item escolhido pelas listas em cascata do QQP.
    internal Guid ItemQqpId { get; init; }

    // Recursos
    internal bool Notebook { get; init; }
    internal bool SegundaTela { get; init; }
    internal bool Celular { get; init; }
    internal bool ExigeCnh { get; init; }
    internal string? CategoriaCnh { get; init; }

    // Segurança
    internal IReadOnlySet<Guid> Racs { get; init; } = new HashSet<Guid>();

    // Contrato: o número vem do corredor (RN13); OS e coletor de custo são preenchidos pelo Solicitante.
    internal string ContratoOs { get; init; } = string.Empty;
    internal string ColetorCusto { get; init; } = string.Empty;
    internal string ResponsavelEfetivoNome { get; init; } = string.Empty;
    internal string ResponsavelEfetivoEmail { get; init; } = string.Empty;
    internal string FiscalEfetivoNome { get; init; } = string.Empty;
    internal string FiscalEfetivoEmail { get; init; } = string.Empty;

    // Outros
    internal string? Observacoes { get; init; }

    /// <summary>
    /// Confere os campos obrigatórios e os limites do modelo físico e devolve uma cópia normalizada:
    /// textos sem espaços nas pontas, opcionais vazios como nulos, período só em vaga temporária e
    /// categoria da CNH só quando exigida. Recusa com todas as mensagens de uma vez.
    /// </summary>
    internal DadosSolicitacao Validar()
    {
        var erros = new List<string>();
        var dados = this with
        {
            AreaSolicitante = Opcional(AreaSolicitante, 150, "Área do solicitante", erros),
            LocalidadeVaga = Obrigatorio(LocalidadeVaga, 200, "Localidade da vaga", erros),
            DescricaoAtividades = Obrigatorio(DescricaoAtividades, null, "Descrição das atividades", erros),
            Formacao = Opcional(Formacao, 300, "Formação exigida", erros),
            PeriodoTemporarioMeses = Temporaria ? PeriodoTemporarioMeses : null,
            CategoriaCnh = ExigeCnh ? CategoriaCnh?.Trim().ToUpperInvariant() : null,
            ContratoOs = Obrigatorio(ContratoOs, 5, "OS do contrato", erros),
            ColetorCusto = Obrigatorio(ColetorCusto, 30, "Coletor de custo", erros),
            ResponsavelEfetivoNome = Obrigatorio(ResponsavelEfetivoNome, 150, "Nome do responsável efetivo", erros),
            ResponsavelEfetivoEmail = Email(ResponsavelEfetivoEmail, "E-mail do responsável efetivo", erros),
            FiscalEfetivoNome = Obrigatorio(FiscalEfetivoNome, 150, "Nome do fiscal efetivo", erros),
            FiscalEfetivoEmail = Email(FiscalEfetivoEmail, "E-mail do fiscal efetivo", erros),
            Observacoes = Opcional(Observacoes, null, "Observações", erros),
            Racs = Racs.ToHashSet(),
        };

        Selecionado(TipoDemandaId, "Tipo de demanda", erros);
        Selecionado(GerenteExecutivoId, "Gerente executivo", erros);
        Selecionado(CorredorId, "Corredor", erros);
        Selecionado(ModeloTrabalhoId, "Modelo de trabalho", erros);
        Selecionado(ItemQqpId, "Cargo e preço (QQP)", erros);

        // Limite da coluna smallint; a regra de negócio é ser maior que zero.
        if (QuantidadeSolicitada is < 1 or > short.MaxValue)
        {
            erros.Add("A quantidade de vagas precisa ser maior que zero.");
        }

        if (Temporaria && PeriodoTemporarioMeses is not (>= 1 and <= short.MaxValue))
        {
            erros.Add("Informe o período temporário, em meses, maior que zero.");
        }

        if (ExigeCnh && !CategoriasCnh.Contains(dados.CategoriaCnh))
        {
            erros.Add("Escolha a categoria da CNH.");
        }

        if (dados.Racs.Contains(Guid.Empty))
        {
            erros.Add("RAC inválida.");
        }

        if (erros.Count > 0)
        {
            throw new RegraNegocioException(string.Join(" ", erros));
        }

        return dados;
    }

    /// <summary>Campos usados no custo (RN12): mudou algum, o custo é recalculado na correção (Cliente).</summary>
    internal bool MudaCusto(DadosSolicitacao outro)
        => ItemQqpId != outro.ItemQqpId
        || QuantidadeSolicitada != outro.QuantidadeSolicitada
        || Notebook != outro.Notebook
        || SegundaTela != outro.SegundaTela
        || Celular != outro.Celular;

    /// <summary>Cada campo com o valor gravado no histórico de alterações; ids de catálogo ficam como id.</summary>
    internal IEnumerable<(string Campo, string? Valor)> Campos()
    {
        yield return (nameof(AreaSolicitante), AreaSolicitante);
        yield return (nameof(TipoDemandaId), Texto(TipoDemandaId));
        yield return (nameof(GerenteExecutivoId), Texto(GerenteExecutivoId));
        yield return (nameof(LocalidadeVaga), LocalidadeVaga);
        yield return (nameof(CorredorId), Texto(CorredorId));
        yield return (nameof(ModeloTrabalhoId), Texto(ModeloTrabalhoId));
        yield return (nameof(QuantidadeSolicitada), Texto(QuantidadeSolicitada));
        yield return (nameof(DescricaoAtividades), DescricaoAtividades);
        yield return (nameof(Formacao), Formacao);
        yield return (nameof(Temporaria), Texto(Temporaria));
        yield return (nameof(PeriodoTemporarioMeses), PeriodoTemporarioMeses is { } meses ? Texto(meses) : null);
        yield return (nameof(ItemQqpId), Texto(ItemQqpId));
        yield return (nameof(Notebook), Texto(Notebook));
        yield return (nameof(SegundaTela), Texto(SegundaTela));
        yield return (nameof(Celular), Texto(Celular));
        yield return (nameof(ExigeCnh), Texto(ExigeCnh));
        yield return (nameof(CategoriaCnh), CategoriaCnh);
        yield return (nameof(Racs), string.Join(";", Racs.Order().Select(Texto)));
        yield return (nameof(ContratoOs), ContratoOs);
        yield return (nameof(ColetorCusto), ColetorCusto);
        yield return (nameof(ResponsavelEfetivoNome), ResponsavelEfetivoNome);
        yield return (nameof(ResponsavelEfetivoEmail), ResponsavelEfetivoEmail);
        yield return (nameof(FiscalEfetivoNome), FiscalEfetivoNome);
        yield return (nameof(FiscalEfetivoEmail), FiscalEfetivoEmail);
        yield return (nameof(Observacoes), Observacoes);
    }

    internal static string Texto(Guid id) => id.ToString();

    internal static string Texto(int numero) => numero.ToString(CultureInfo.InvariantCulture);

    internal static string Texto(bool valor) => valor ? "Sim" : "Não";

    internal static string Texto(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Obrigatorio(string? texto, int? maximo, string nome, List<string> erros)
    {
        var valor = texto?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            erros.Add($"{nome} é obrigatório.");
        }
        else if (valor.Length > maximo)
        {
            erros.Add($"{nome} tem no máximo {maximo} caracteres.");
        }

        return valor;
    }

    private static string? Opcional(string? texto, int? maximo, string nome, List<string> erros)
    {
        var valor = texto?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (valor.Length > maximo)
        {
            erros.Add($"{nome} tem no máximo {maximo} caracteres.");
        }

        return valor;
    }

    // Contato, não usuário: qualquer domínio (seção 8.1).
    private static string Email(string? texto, string nome, List<string> erros)
    {
        var valor = Obrigatorio(texto, 254, nome, erros).ToLowerInvariant();
        if (valor.Length is > 0 and <= 254 && !FormatoEmail().IsMatch(valor))
        {
            erros.Add($"{nome} não é válido.");
        }

        return valor;
    }

    private static void Selecionado(Guid id, string nome, List<string> erros)
    {
        if (id == Guid.Empty)
        {
            erros.Add($"{nome} é obrigatório.");
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoEmail();
}
