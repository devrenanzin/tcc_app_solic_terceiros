using Contratacao.Web.Domain.Contratos;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>Item do catálogo QQP escolhido, com o piso e o preço vigentes no momento do cálculo.</summary>
internal sealed record PrecoQqp(Guid ItemQqpId, decimal PisoSalarial, decimal PrecoUnitario);

/// <summary>Valores mensais por pessoa dos equipamentos do catálogo (RN12), editáveis pelo Admin.</summary>
internal sealed record ValoresEquipamentos(decimal Notebook, decimal SegundaTela, decimal Celular);

/// <summary>
/// O que o envio e a correção leem dos catálogos para os campos escolhidos: o corredor e o seu contrato
/// (RN13), a OS escolhida, o preço do item QQP e os valores dos equipamentos (RN12), todos com os valores atuais.
/// </summary>
internal sealed record ReferenciasSolicitacao(
    Corredor Corredor, Contrato Contrato, OrdemServico Os, PrecoQqp Qqp, ValoresEquipamentos Equipamentos);

/// <summary>RN12 — Custo total mensal = Quantidade × (Preço unitário QQP + Equipamentos por pessoa).</summary>
internal static class CustoDemanda
{
    /// <summary>Soma dos equipamentos marcados no formulário: notebook, segunda tela e celular.</summary>
    internal static decimal EquipamentosPorPessoa(bool notebook, bool segundaTela, bool celular, ValoresEquipamentos valores)
        => (notebook ? valores.Notebook : 0m)
        + (segundaTela ? valores.SegundaTela : 0m)
        + (celular ? valores.Celular : 0m);

    internal static decimal Total(int quantidade, decimal precoUnitarioQqp, decimal equipamentosPorPessoa)
        => decimal.Round(quantidade * (precoUnitarioQqp + equipamentosPorPessoa), 2, MidpointRounding.AwayFromZero);
}
