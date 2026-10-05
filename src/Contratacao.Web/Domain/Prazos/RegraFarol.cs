namespace Contratacao.Web.Domain.Prazos;

/// <summary>Tabela do farol (seções 9–11).</summary>
internal static class RegraFarol
{
    private const int LimiteVerde = 10;
    private const int LimiteAmarelo = 6;

    /// <param name="sla">Nulo enquanto o Gestor não aprovou.</param>
    /// <param name="cancelada">Demanda cancelada: cinza, com o rótulo Cancelado.</param>
    /// <param name="dataFinalizacaoLocal">Data da finalização em Brasília, se finalizada.</param>
    /// <param name="hoje">Data de hoje em Brasília.</param>
    internal static Farol Calcular(Sla? sla, bool cancelada, DateOnly? dataFinalizacaoLocal, DateOnly hoje)
    {
        // SUPOSIÇÃO (S7): demanda cancelada fica cinza.
        if (cancelada || sla is null)
        {
            return Farol.Cinza;
        }

        if (dataFinalizacaoLocal is { } finalizacao)
        {
            return finalizacao <= sla.DataLimite ? Farol.Verde : Farol.Vermelho;
        }

        var restantes = sla.DiasRestantes(hoje);

        // SUPOSIÇÃO (S7): o dia limite (0 restantes) fica laranja.
        return restantes switch
        {
            > LimiteVerde => Farol.Verde,
            >= LimiteAmarelo => Farol.Amarelo,
            >= 0 => Farol.Laranja,
            _ => Farol.Vermelho,
        };
    }
}
