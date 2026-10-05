using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Catalogos;

// Catálogos do formulário (seção 23), mantidos pelo Admin pela tela de parâmetros.

internal sealed class TipoDemanda
{
    private TipoDemanda() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }
}

/// <summary>
/// Gerente responsável pela área; vazio na carga inicial (são nomes de pessoas) e cadastrado pelo Admin
/// (seção 23). Não é excluído: o desativado sai da lista do formulário e continua nas demandas antigas.
/// </summary>
internal sealed class GerenteExecutivo
{
    private const int TamanhoMaximoNome = 150;

    private GerenteExecutivo() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }

    internal static GerenteExecutivo Cadastrar(Ator admin, string nome)
    {
        ExigirAdmin(admin);
        var valor = nome?.Trim() ?? string.Empty;
        if (valor.Length is 0 or > TamanhoMaximoNome)
        {
            throw new RegraNegocioException($"O nome é obrigatório e tem no máximo {TamanhoMaximoNome} caracteres.");
        }

        return new GerenteExecutivo { Nome = valor, Ativo = true };
    }

    internal void AlterarSituacao(Ator admin, bool ativo)
    {
        ExigirAdmin(admin);
        if (Ativo == ativo)
        {
            throw new RegraNegocioException(ativo ? "O gerente já está ativo." : "O gerente já está desativado.");
        }

        Ativo = ativo;
    }

    private static void ExigirAdmin(Ator ator)
    {
        // Catálogos do formulário são mantidos pelo Admin (seções 19 e 23).
        if (!ator.Eh(Perfil.Admin))
        {
            throw new RegraNegocioException("Só o Admin mantém os gerentes executivos.");
        }
    }
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
