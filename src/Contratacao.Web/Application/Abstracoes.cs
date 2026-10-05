using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application;

// Portas da camada Application. As implementações ficam em Infrastructure, para que Application
// dependa só de Domain (seção 27).

internal interface IUsuarios
{
    Task<Usuario?> ObterAsync(Guid id, CancellationToken cancelamento);
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancelamento);
    Task<bool> EmailEmUsoAsync(string emailNormalizado, CancellationToken cancelamento);
    void Adicionar(Usuario usuario);

    Task<IReadOnlyList<Guid>> ContratosDoGestorAsync(Guid gestorId, CancellationToken cancelamento);
    Task DefinirContratosDoGestorAsync(Guid gestorId, IReadOnlyCollection<Guid> contratos, CancellationToken cancelamento);

    /// <summary>Lista para as telas de cadastro; filtra por perfil e, opcionalmente, pelo Gestor responsável.</summary>
    Task<IReadOnlyList<ResumoUsuario>> ListarAsync(Perfil perfil, Guid? gestorResponsavelId, CancellationToken cancelamento);
}

internal interface IContratos
{
    Task<IReadOnlyList<ResumoContrato>> ListarAtivosAsync(CancellationToken cancelamento);
}

internal interface IAuditoria
{
    void Registrar(LogAuditoria registro);
}

internal interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken cancelamento);
}

internal interface IHashSenha
{
    string Gerar(Usuario usuario, string senha);
    bool Conferir(Usuario usuario, string senha);
}

internal sealed record ResumoContrato(Guid Id, string Numero);

internal sealed record ResumoUsuario(
    Guid Id,
    string Nome,
    string Email,
    Perfil Perfil,
    bool Ativo,
    DateTime DataCadastro,
    DateTime? DataUltimoAcesso,
    IReadOnlyList<string> Contratos,
    Guid? GestorResponsavelId,
    string? GestorResponsavelNome);
