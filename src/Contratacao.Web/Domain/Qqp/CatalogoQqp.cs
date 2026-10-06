namespace Contratacao.Web.Domain.Qqp;

// Catálogo QQP da Gerenciadora (seção 23): cada combinação de região, função, classificação, nível e carga horária
// tem um piso salarial e um preço unitário.

internal sealed class QqpRegiao
{
    private QqpRegiao() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
}

internal sealed class QqpFuncao
{
    private QqpFuncao() { } // EF Core

    internal QqpFuncao(string nome) => Nome = nome;

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
}

internal sealed class QqpClassificacao
{
    private QqpClassificacao() { } // EF Core

    internal QqpClassificacao(string nome) => Nome = nome;

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
}

internal sealed class QqpNivel
{
    private QqpNivel() { } // EF Core

    internal QqpNivel(string nome, short ordem)
    {
        Nome = nome;
        Ordem = ordem;
    }

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal short Ordem { get; private set; }
}

internal sealed class QqpCargaHoraria
{
    private QqpCargaHoraria() { } // EF Core

    internal QqpCargaHoraria(short horasSemanais) => HorasSemanais = horasSemanais;

    internal Guid Id { get; private set; }
    internal short HorasSemanais { get; private set; }
}

/// <summary>Item de preço do QQP. O código é a chave; a combinação não é única (códigos 466 e 467).</summary>
internal sealed class ItemQqp
{
    private ItemQqp() { } // EF Core

    internal ItemQqp(
        int codigo,
        Guid regiaoId,
        Guid funcaoId,
        Guid? classificacaoId,
        Guid nivelId,
        Guid cargaHorariaId,
        decimal pisoSalarial,
        decimal precoUnitario)
    {
        Codigo = codigo;
        RegiaoId = regiaoId;
        FuncaoId = funcaoId;
        ClassificacaoId = classificacaoId;
        NivelId = nivelId;
        CargaHorariaId = cargaHorariaId;
        PisoSalarial = pisoSalarial;
        PrecoUnitario = precoUnitario;
        Ativo = true;
    }

    internal Guid Id { get; private set; }
    internal int Codigo { get; private set; }
    internal Guid RegiaoId { get; private set; }
    internal Guid FuncaoId { get; private set; }

    /// <summary>Nulo quando a planilha traz "-" (sem classificação).</summary>
    internal Guid? ClassificacaoId { get; private set; }

    internal Guid NivelId { get; private set; }
    internal Guid CargaHorariaId { get; private set; }
    internal decimal PisoSalarial { get; private set; }
    internal decimal PrecoUnitario { get; private set; }
    internal bool Ativo { get; private set; }
}
