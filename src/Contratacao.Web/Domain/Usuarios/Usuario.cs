using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Usuarios;

/// <summary>
/// Usuário do sistema (seções 4 e 4.5). Nunca é excluído, só desativado.
/// As regras de cadastro por perfil entram na Etapa 3.
/// </summary>
internal sealed class Usuario
{
    private const int TamanhoMaximoNome = 150;

    private Usuario() { } // EF Core

    internal Guid Id { get; private set; }
    internal Perfil Perfil { get; private set; }

    /// <summary>Gestor que gerencia o cadastro; só para Funcionário SESI.</summary>
    internal Guid? GestorResponsavelId { get; private set; }

    internal Guid? CriadoPorUsuarioId { get; private set; }

    /// <summary>Contrato do grupo do Funcionário SESI.</summary>
    internal Guid? ContratoId { get; private set; }

    internal string Nome { get; private set; } = string.Empty;
    internal string Email { get; private set; } = string.Empty;

    /// <summary>Igual ao e-mail completo, usado para entrar no sistema.</summary>
    internal string Login { get; private set; } = string.Empty;
    internal bool Ativo { get; private set; }
    internal DateTime DataCadastro { get; private set; }
    internal DateTime? DataUltimoAcesso { get; private set; }

    /// <summary>Só o hash, gerado pelo mecanismo padrão do ASP.NET Core; a senha nunca é gravada.</summary>
    internal string SenhaHash { get; private set; } = string.Empty;

    /// <summary>Admin inicial, criado pela preparação do banco a partir da configuração (seção 23).</summary>
    internal static Usuario CriarAdminInicial(string nome, string email, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > TamanhoMaximoNome)
        {
            throw new RegraNegocioException($"O nome é obrigatório e tem no máximo {TamanhoMaximoNome} caracteres.");
        }

        var emailNormalizado = EmailUcl.Normalizar(email);
        return new Usuario
        {
            Perfil = Perfil.Admin,
            Nome = nome.Trim(),
            Email = emailNormalizado,
            // O login é o e-mail completo (Cliente, revisão de 05/10/2026).
            Login = emailNormalizado,
            Ativo = true,
            DataCadastro = agoraUtc,
        };
    }

    internal void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new RegraNegocioException("O hash da senha é obrigatório.");
        }

        SenhaHash = senhaHash;
    }
}
