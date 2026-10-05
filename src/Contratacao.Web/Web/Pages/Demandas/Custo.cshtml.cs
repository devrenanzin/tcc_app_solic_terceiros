using Contratacao.Web.Application.Demandas;

namespace Contratacao.Web.Web.Pages.Demandas;

/// <summary>Prévia do custo (RN12) calculada pelo domínio a cada mudança de cargo, quantidade ou equipamentos.</summary>
internal sealed class CustoModel(CalcularPreviaCusto calcular) : PaginaBase
{
    internal PreviaCusto Previa { get; private set; } = new(null, null, 0m, null);
    internal int? Quantidade { get; private set; }

    public async Task OnGetAsync(EntradaDemanda entrada)
    {
        // Campos inválidos (por exemplo, quantidade com letras) só deixam a prévia sem total.
        Quantidade = entrada.QuantidadeSolicitada;
        Previa = await calcular.ExecutarAsync(
            entrada.ItemQqpId, entrada.QuantidadeSolicitada, entrada.Notebook, entrada.SegundaTela, entrada.Celular, Cancelamento);
    }
}
