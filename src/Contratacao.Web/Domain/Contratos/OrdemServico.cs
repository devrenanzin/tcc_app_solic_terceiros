using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Contratos;

/// <summary>
/// OS de um contrato (Cliente, revisão de 06/10/2026): cada OS pertence a um contrato e o formulário só oferece
/// as OS do contrato do corredor. A mesma OS pode ter coletores de custo diferentes, por isso o coletor fica na
/// demanda. Cadastrada pelo Admin; não é excluída, só desativada.
/// </summary>
internal sealed class OrdemServico
{
    internal const int TamanhoMaximoNumero = 5;

    private OrdemServico() { } // EF Core

    internal Guid Id { get; private set; }
    internal Guid ContratoId { get; private set; }
    internal string Numero { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }

    internal static OrdemServico Cadastrar(Ator admin, Guid contratoId, string numero)
    {
        ExigirAdmin(admin);
        Exigir(contratoId != Guid.Empty, "Escolha o contrato da OS.");
        var valor = numero?.Trim() ?? string.Empty;
        Exigir(valor.Length is > 0 and <= TamanhoMaximoNumero, $"O número da OS é obrigatório e tem no máximo {TamanhoMaximoNumero} caracteres.");
        return new OrdemServico { ContratoId = contratoId, Numero = valor, Ativo = true };
    }

    /// <summary>Para os testes do domínio; as OS reais são cadastradas pelo Admin.</summary>
    internal static OrdemServico Criar(Guid id, Guid contratoId, string numero, bool ativo = true)
        => new() { Id = id, ContratoId = contratoId, Numero = numero, Ativo = ativo };

    internal void AlterarSituacao(Ator admin, bool ativo)
    {
        ExigirAdmin(admin);
        Exigir(Ativo != ativo, ativo ? "A OS já está ativa." : "A OS já está desativada.");
        Ativo = ativo;
    }

    private static void ExigirAdmin(Ator ator) => Exigir(ator.Eh(Perfil.Admin), "Só o Admin mantém as OS dos contratos.");

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new RegraNegocioException(mensagem);
        }
    }
}
