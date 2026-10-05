namespace Contratacao.Web.Domain.Comum;

/// <summary>Ação recusada por uma regra de negócio: transição proibida, ator sem permissão ou dado inválido.</summary>
internal sealed class RegraNegocioException(string mensagem) : Exception(mensagem);
