namespace Contratacao.Web.Web.Pages.Shared;

/// <summary>Modelo do botão de desativar ou reativar um usuário (_AcaoSituacao).</summary>
internal sealed record AcaoSituacao(Guid Id, bool Ativo, string Nome);

/// <summary>Modelo da lista de contratos para marcar (_EscolhaContratos).</summary>
internal sealed record EscolhaContratos(IReadOnlyList<Application.ResumoContrato> Contratos, IReadOnlyCollection<Guid> Marcados, string Campo, bool Multipla);
