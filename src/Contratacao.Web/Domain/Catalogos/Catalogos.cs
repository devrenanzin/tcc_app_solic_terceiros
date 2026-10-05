namespace Contratacao.Web.Domain.Catalogos;

// Catálogos do formulário (seção 23), mantidos pelo Admin pela tela de parâmetros.

internal sealed class TipoDemanda
{
    private TipoDemanda() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }
}

/// <summary>Gerente responsável pela área; vazio na carga inicial (são nomes de pessoas).</summary>
internal sealed class GerenteExecutivo
{
    private GerenteExecutivo() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }
}

internal sealed class ModeloTrabalho
{
    private ModeloTrabalho() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
}

/// <summary>
/// Itens cobrados por pessoa no custo (RN12): notebook, segunda tela e celular.
/// Veículo e rastreador saíram do MVP (revisão de 05/10/2026, item 42).
/// </summary>
internal sealed class ItemEquipamento
{
    private ItemEquipamento() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal decimal Valor { get; private set; }
    internal bool Ativo { get; private set; }
}

/// <summary>Regra de atividade crítica (lista tb_racs).</summary>
internal sealed class Rac
{
    private Rac() { } // EF Core

    internal Rac(string codigo, string nome)
    {
        Codigo = codigo;
        Nome = nome;
    }

    internal Guid Id { get; private set; }
    internal string Codigo { get; private set; } = string.Empty;
    internal string Nome { get; private set; } = string.Empty;
}
