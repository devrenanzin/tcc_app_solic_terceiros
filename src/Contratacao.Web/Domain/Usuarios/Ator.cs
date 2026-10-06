namespace Contratacao.Web.Domain.Usuarios;

/// <summary>
/// Quem executa uma ação: usuário, perfil, contratos a que está vinculado e IP da requisição,
/// usados nas permissões e no registro de auditoria.
/// </summary>
/// <param name="Contratos">
/// Gestor: os contratos da GestorContrato (um ou mais). Funcionário da Gerenciadora: o contrato do seu grupo.
/// Admin e Solicitante: vazio.
/// </param>
internal sealed record Ator(Guid Id, Perfil Perfil, bool Ativo, IReadOnlySet<Guid> Contratos, string? EnderecoIp)
{
    internal bool Eh(Perfil perfil) => Ativo && Perfil == perfil;

    internal bool AtuaNoContrato(Guid contratoId) => Ativo && Contratos.Contains(contratoId);
}
