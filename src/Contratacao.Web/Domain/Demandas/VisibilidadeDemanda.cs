using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>
/// Quais demandas cada perfil vê (seções 4 e 19): Admin e Gestor, todas; Solicitante, as que criou;
/// Funcionário da Gerenciadora, as do contrato do seu grupo. Usuário desativado não vê nenhuma.
/// </summary>
/// <param name="SolicitanteId">Quando preenchido, só as demandas deste Solicitante.</param>
/// <param name="Contratos">Quando preenchido, só as demandas destes contratos.</param>
internal sealed record FiltroVisibilidade(bool Nenhuma, Guid? SolicitanteId, IReadOnlySet<Guid>? Contratos)
{
    internal static FiltroVisibilidade Todas { get; } = new(false, null, null);

    internal static FiltroVisibilidade Bloqueado { get; } = new(true, null, null);

    internal static FiltroVisibilidade Para(Ator ator) => ator switch
    {
        { Ativo: false } => Bloqueado,
        { Perfil: Perfil.Admin or Perfil.Gestor } => Todas,
        { Perfil: Perfil.Solicitante } => new(false, ator.Id, null),
        { Perfil: Perfil.FuncionarioSesi } => new(false, null, ator.Contratos),
        _ => Bloqueado,
    };

    internal bool Inclui(Guid solicitanteId, Guid contratoId)
        => !Nenhuma
        && (SolicitanteId is not { } solicitante || solicitante == solicitanteId)
        && (Contratos is null || Contratos.Contains(contratoId));
}
