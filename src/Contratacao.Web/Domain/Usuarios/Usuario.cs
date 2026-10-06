using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Usuarios;

/// <summary>
/// Usuário do sistema (seções 4 e 4.5). Nunca é excluído, só desativado.
/// Quem cadastra e gerencia quem: Admin → Gestores e qualquer Funcionário SESI; Gestor → Funcionários SESI da sua
/// equipe; o Solicitante se cadastra sozinho e é desativado pelo Admin ou por qualquer Gestor. Uma conta de
/// Solicitante pode ser vinculada como Funcionário SESI ou Gestor, mudando o perfil (Cliente, revisão de 06/10/2026).
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

    /// <summary>
    /// Senha inicial definida por quem cadastrou (Gestor e Funcionário SESI): o usuário troca no primeiro
    /// acesso, antes de usar o sistema (Cliente, revisão de 05/10/2026).
    /// </summary>
    internal bool DeveTrocarSenha { get; private set; }

    /// <summary>Admin inicial, criado pela preparação do banco a partir da configuração (seção 23).</summary>
    internal static Usuario CriarAdminInicial(string nome, string email, DateTime agoraUtc)
        => Novo(Perfil.Admin, nome, email, agoraUtc, criadoPorId: null);

    /// <summary>Autocadastro do Solicitante (UC13): qualquer pessoa com e-mail @ucl.br.</summary>
    internal static Usuario CadastrarSolicitante(string nome, string email, DateTime agoraUtc)
        => Novo(Perfil.Solicitante, nome, email, agoraUtc, criadoPorId: null);

    /// <summary>Cadastro de Gestor pelo Admin (UC12). Os contratos ficam em GestorContrato.</summary>
    internal static Usuario CadastrarGestor(Ator admin, string nome, string email, DateTime agoraUtc)
    {
        Exigir(admin.Eh(Perfil.Admin), "Só o Admin cadastra Gestores.");
        var gestor = Novo(Perfil.Gestor, nome, email, agoraUtc, admin.Id);
        gestor.DeveTrocarSenha = true;
        return gestor;
    }

    /// <summary>
    /// Cadastro de Funcionário SESI (UC14): pelo Gestor, na sua equipe e num dos seus contratos; pelo Admin, na
    /// equipe de qualquer Gestor ativo e em qualquer contrato (Cliente, revisão de 06/10/2026).
    /// </summary>
    internal static Usuario CadastrarFuncionarioSesi(
        Ator ator, string nome, string email, Guid contratoId, Ator gestorResponsavel, DateTime agoraUtc)
    {
        ExigirQuemVinculaSesi(ator, contratoId, gestorResponsavel);

        var usuario = Novo(Perfil.FuncionarioSesi, nome, email, agoraUtc, ator.Id);
        usuario.GestorResponsavelId = gestorResponsavel.Id;
        usuario.ContratoId = contratoId;
        usuario.DeveTrocarSenha = true;
        return usuario;
    }

    /// <summary>
    /// Vincula uma conta já cadastrada de Solicitante como Funcionário SESI: o perfil muda, com o mesmo e-mail e senha
    /// (Cliente, revisão de 06/10/2026). Mesmas regras do cadastro: o Gestor, na própria equipe e nos seus contratos;
    /// o Admin, para qualquer Gestor e contrato. Recusado se a conta tem demandas em andamento, porque só o
    /// Solicitante as corrige.
    /// </summary>
    internal void TornarFuncionarioSesi(Ator ator, Guid contratoId, Ator gestorResponsavel, int demandasEmAndamento)
    {
        ExigirContaDeSolicitante(demandasEmAndamento);
        ExigirQuemVinculaSesi(ator, contratoId, gestorResponsavel);

        Perfil = Perfil.FuncionarioSesi;
        GestorResponsavelId = gestorResponsavel.Id;
        ContratoId = contratoId;
    }

    /// <summary>Vincula uma conta já cadastrada de Solicitante como Gestor, pelo Admin; os contratos ficam em GestorContrato.</summary>
    internal void TornarGestor(Ator admin, int demandasEmAndamento)
    {
        Exigir(admin.Eh(Perfil.Admin), "Só o Admin vincula uma conta como Gestor.");
        ExigirContaDeSolicitante(demandasEmAndamento);
        Perfil = Perfil.Gestor;
    }

    private void ExigirContaDeSolicitante(int demandasEmAndamento)
    {
        Exigir(Perfil == Perfil.Solicitante && Ativo, "Só uma conta ativa de Solicitante pode ser vinculada a outro perfil.");
        Exigir(demandasEmAndamento == 0,
            "Esta conta tem demandas em andamento como Solicitante. Conclua ou cancele essas demandas antes de mudar o perfil.");
    }

    /// <summary>Gestor: só na própria equipe e nos seus contratos. Admin: qualquer Gestor ativo e qualquer contrato.</summary>
    private static void ExigirQuemVinculaSesi(Ator ator, Guid contratoId, Ator gestorResponsavel)
    {
        Exigir(ator.Eh(Perfil.Gestor) || ator.Eh(Perfil.Admin), "Só um Gestor ativo ou o Admin cadastra Funcionários SESI.");
        Exigir(contratoId != Guid.Empty, "Escolha o contrato do Funcionário SESI.");
        Exigir(gestorResponsavel.Eh(Perfil.Gestor), "Escolha um Gestor ativo como responsável.");

        if (ator.Eh(Perfil.Gestor))
        {
            Exigir(gestorResponsavel.Id == ator.Id, "O Gestor cadastra Funcionários SESI só na própria equipe.");
            Exigir(ator.AtuaNoContrato(contratoId), "Escolha um dos seus contratos. Para outro contrato, peça ao Admin.");
        }
    }

    /// <summary>
    /// Quem gerencia o cadastro deste usuário (seções 4.5 e 19): o Admin gerencia Gestores e qualquer Funcionário
    /// SESI (Cliente); o Gestor, só os Funcionários SESI da sua equipe; Solicitantes, o Admin e qualquer Gestor.
    /// Ninguém gerencia o Admin.
    /// </summary>
    internal bool PodeSerGerenciadoPor(Ator ator) => Perfil switch
    {
        Perfil.Gestor => ator.Eh(Perfil.Admin),
        Perfil.FuncionarioSesi => ator.Eh(Perfil.Admin) || (ator.Eh(Perfil.Gestor) && GestorResponsavelId == ator.Id),
        Perfil.Solicitante => ator.Eh(Perfil.Admin) || ator.Eh(Perfil.Gestor),
        _ => false,
    };

    internal void Desativar(Ator ator)
    {
        Exigir(PodeSerGerenciadoPor(ator), "Você não gerencia o cadastro deste usuário.");
        Exigir(Ativo, "O usuário já está desativado.");
        Ativo = false;
    }

    internal void Reativar(Ator ator)
    {
        Exigir(PodeSerGerenciadoPor(ator), "Você não gerencia o cadastro deste usuário.");
        Exigir(!Ativo, "O usuário já está ativo.");
        Ativo = true;
    }

    /// <summary>
    /// Troca o contrato do grupo do Funcionário SESI (Cliente, revisão de 05/10/2026): o Gestor responsável,
    /// só entre os seus contratos; o Admin, para qualquer contrato.
    /// </summary>
    internal void AlterarContrato(Ator ator, Guid contratoId)
    {
        Exigir(Perfil == Perfil.FuncionarioSesi, "Só Funcionários SESI têm contrato próprio.");
        Exigir(contratoId != Guid.Empty, "Escolha o contrato.");

        if (!ator.Eh(Perfil.Admin))
        {
            Exigir(PodeSerGerenciadoPor(ator), "Você não gerencia o cadastro deste usuário.");
            Exigir(ator.AtuaNoContrato(contratoId), "Escolha um dos seus contratos. Para outro contrato, peça ao Admin.");
        }

        ContratoId = contratoId;
    }

    /// <summary>Troca de senha pelo próprio usuário; encerra a obrigação do primeiro acesso.</summary>
    internal void TrocarSenha(string novoHash)
    {
        DefinirSenhaHash(novoHash);
        DeveTrocarSenha = false;
    }

    /// <summary>
    /// Transferência do Funcionário SESI para outro Gestor (UC19), pelo Admin.
    /// A equipe de um Gestor desativado continua ativa e pode ser transferida (Cliente).
    /// </summary>
    internal void TransferirPara(Ator admin, Usuario novoGestor)
    {
        Exigir(admin.Eh(Perfil.Admin), "Só o Admin transfere o vínculo de usuários.");
        Exigir(Perfil == Perfil.FuncionarioSesi, "Só Funcionários SESI têm Gestor responsável.");
        Exigir(novoGestor.Perfil == Perfil.Gestor && novoGestor.Ativo, "O destino precisa ser um Gestor ativo.");
        Exigir(novoGestor.Id != GestorResponsavelId, "O usuário já pertence a este Gestor.");
        GestorResponsavelId = novoGestor.Id;
    }

    internal void RegistrarAcesso(DateTime agoraUtc) => DataUltimoAcesso = agoraUtc;

    internal void DefinirSenhaHash(string senhaHash)
    {
        Exigir(!string.IsNullOrWhiteSpace(senhaHash), "O hash da senha é obrigatório.");
        SenhaHash = senhaHash;
    }

    private static Usuario Novo(Perfil perfil, string nome, string email, DateTime agoraUtc, Guid? criadoPorId)
    {
        Exigir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= TamanhoMaximoNome,
            $"O nome é obrigatório e tem no máximo {TamanhoMaximoNome} caracteres.");

        var emailNormalizado = EmailUcl.Normalizar(email);
        return new Usuario
        {
            Perfil = perfil,
            Nome = nome.Trim(),
            Email = emailNormalizado,
            // O login é o e-mail completo (Cliente, revisão de 05/10/2026).
            Login = emailNormalizado,
            Ativo = true,
            DataCadastro = agoraUtc,
            CriadoPorUsuarioId = criadoPorId,
        };
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new RegraNegocioException(mensagem);
        }
    }
}
