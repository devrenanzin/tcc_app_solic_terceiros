namespace Contratacao.Web.Domain.Contratos;

/// <summary>Combinação de corredor e região; define o contrato da demanda (RN13).</summary>
internal sealed class Corredor
{
    private Corredor() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Nome { get; private set; } = string.Empty;

    /// <summary>Região do catálogo QQP (QQP NORTE ou QQP SUDESTE).</summary>
    internal Guid RegiaoId { get; private set; }

    internal Guid ContratoId { get; private set; }
    internal bool Ativo { get; private set; }

    /// <summary>Os corredores vêm da carga inicial; a fábrica serve aos testes do domínio.</summary>
    internal static Corredor Criar(Guid id, string nome, Guid regiaoId, Guid contratoId, bool ativo = true)
        => new() { Id = id, Nome = nome, RegiaoId = regiaoId, ContratoId = contratoId, Ativo = ativo };
}
