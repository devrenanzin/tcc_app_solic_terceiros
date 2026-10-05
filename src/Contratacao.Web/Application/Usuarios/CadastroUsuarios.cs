using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Usuarios;

/// <summary>UC12 — Cadastrar Gestor, pelo Admin, vinculado a um ou mais contratos.</summary>
internal sealed class CadastrarGestor(
    IUsuarios usuarios, IContratos contratos, IHashSenha hash, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task<Usuario> ExecutarAsync(
        Ator admin, string nome, string email, string senhaInicial, IReadOnlyCollection<Guid> contratosDoGestor, CancellationToken cancelamento)
    {
        // Quem cadastra define a senha inicial; o usuário a troca no primeiro acesso (Cliente).
        Senha.Validar(senhaInicial);
        var gestor = Usuario.CadastrarGestor(admin, nome, email, relogio.AgoraUtc);
        await CadastrarSolicitante.GarantirEmailLivreAsync(usuarios, gestor.Email, cancelamento);
        await ValidacaoContratos.ExigirAtivosAsync(contratos, contratosDoGestor, cancelamento);

        gestor.DefinirSenhaHash(hash.Gerar(gestor, senhaInicial));
        usuarios.Adicionar(gestor);
        await usuarios.DefinirContratosDoGestorAsync(gestor.Id, contratosDoGestor, cancelamento);

        var numeros = await ValidacaoContratos.NumerosAsync(contratos, contratosDoGestor, cancelamento);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(Usuario), gestor.Id, "CadastroGestor",
            null, $"{gestor.Email}; contratos: {numeros}", null, relogio.AgoraUtc));

        await unidade.SalvarAsync(cancelamento);
        return gestor;
    }
}

/// <summary>UC14 — Cadastrar Funcionário SESI, pelo Gestor, na sua equipe e no grupo de um contrato.</summary>
internal sealed class CadastrarFuncionarioSesi(
    IUsuarios usuarios, IContratos contratos, IHashSenha hash, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task<Usuario> ExecutarAsync(
        Ator gestor, string nome, string email, string senhaInicial, Guid contratoId, CancellationToken cancelamento)
    {
        // Quem cadastra define a senha inicial; o usuário a troca no primeiro acesso (Cliente).
        Senha.Validar(senhaInicial);
        var funcionario = Usuario.CadastrarFuncionarioSesi(gestor, nome, email, contratoId, relogio.AgoraUtc);
        await CadastrarSolicitante.GarantirEmailLivreAsync(usuarios, funcionario.Email, cancelamento);

        await ValidacaoContratos.ExigirAtivosAsync(contratos, [contratoId], cancelamento);

        funcionario.DefinirSenhaHash(hash.Gerar(funcionario, senhaInicial));
        usuarios.Adicionar(funcionario);

        var numero = await ValidacaoContratos.NumerosAsync(contratos, [contratoId], cancelamento);
        auditoria.Registrar(LogAuditoria.De(gestor, nameof(Usuario), funcionario.Id, "CadastroFuncionarioSesi",
            null, $"{funcionario.Email}; contrato: {numero}", null, relogio.AgoraUtc));

        await unidade.SalvarAsync(cancelamento);
        return funcionario;
    }
}

/// <summary>Desativa ou reativa um usuário; usuários nunca são excluídos (seção 4.5).</summary>
internal sealed class AlterarSituacaoUsuario(IUsuarios usuarios, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator ator, Guid usuarioId, bool ativo, CancellationToken cancelamento)
    {
        var usuario = await usuarios.ObterAsync(usuarioId, cancelamento)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        if (ativo)
        {
            usuario.Reativar(ator);
        }
        else
        {
            usuario.Desativar(ator);
        }

        auditoria.Registrar(LogAuditoria.De(ator, nameof(Usuario), usuario.Id, ativo ? "Reativacao" : "Desativacao",
            ativo ? "Inativo" : "Ativo", ativo ? "Ativo" : "Inativo", null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>Contratos de um Gestor, definidos pelo Admin (revisão de 05/10/2026). Pelo menos um.</summary>
internal sealed class DefinirContratosGestor(
    IUsuarios usuarios, IContratos contratos, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator admin, Guid gestorId, IReadOnlyCollection<Guid> novos, CancellationToken cancelamento)
    {
        var gestor = await usuarios.ObterAsync(gestorId, cancelamento);
        if (gestor is not { Perfil: Perfil.Gestor } || !gestor.PodeSerGerenciadoPor(admin))
        {
            throw new RegraNegocioException("Só o Admin define os contratos de um Gestor.");
        }

        await ValidacaoContratos.ExigirAtivosAsync(contratos, novos, cancelamento);
        var anteriores = await usuarios.ContratosDoGestorAsync(gestorId, cancelamento);
        await usuarios.DefinirContratosDoGestorAsync(gestorId, novos, cancelamento);

        auditoria.Registrar(LogAuditoria.De(admin, "GestorContrato", gestorId, "AlteracaoContratosGestor",
            await ValidacaoContratos.NumerosAsync(contratos, anteriores, cancelamento),
            await ValidacaoContratos.NumerosAsync(contratos, novos, cancelamento),
            null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>Troca o contrato do grupo de um Funcionário SESI, pelo Gestor responsável (UC14).</summary>
internal sealed class AlterarContratoFuncionarioSesi(
    IUsuarios usuarios, IContratos contratos, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator gestor, Guid funcionarioId, Guid contratoId, CancellationToken cancelamento)
    {
        var funcionario = await usuarios.ObterAsync(funcionarioId, cancelamento)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        await ValidacaoContratos.ExigirAtivosAsync(contratos, [contratoId], cancelamento);

        var anterior = funcionario.ContratoId;
        funcionario.AlterarContrato(gestor, contratoId);

        auditoria.Registrar(LogAuditoria.De(gestor, nameof(Usuario), funcionario.Id, "AlteracaoContratoSesi",
            anterior is { } a ? await ValidacaoContratos.NumerosAsync(contratos, [a], cancelamento) : null,
            await ValidacaoContratos.NumerosAsync(contratos, [contratoId], cancelamento),
            null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>UC19 — Transferir Vínculo de Usuário: Funcionários SESI de um Gestor para outro, com justificativa.</summary>
internal sealed class TransferirVinculo(IUsuarios usuarios, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    private const int TamanhoMaximoJustificativa = 1000;

    internal async Task ExecutarAsync(
        Ator admin, IReadOnlyCollection<Guid> funcionarios, Guid gestorDestinoId, string justificativa, CancellationToken cancelamento)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length > TamanhoMaximoJustificativa)
        {
            throw new RegraNegocioException($"A justificativa é obrigatória e tem no máximo {TamanhoMaximoJustificativa} caracteres.");
        }

        if (funcionarios.Count == 0)
        {
            throw new RegraNegocioException("Escolha pelo menos um Funcionário SESI.");
        }

        var destino = await usuarios.ObterAsync(gestorDestinoId, cancelamento)
            ?? throw new RegraNegocioException("Gestor de destino não encontrado.");

        foreach (var id in funcionarios.Distinct())
        {
            var funcionario = await usuarios.ObterAsync(id, cancelamento)
                ?? throw new RegraNegocioException("Usuário não encontrado.");
            var anterior = funcionario.GestorResponsavelId;

            funcionario.TransferirPara(admin, destino);

            auditoria.Registrar(LogAuditoria.De(admin, nameof(Usuario), funcionario.Id, "TransferenciaVinculo",
                anterior?.ToString(), destino.Id.ToString(), justificativa.Trim(), relogio.AgoraUtc));
        }

        await unidade.SalvarAsync(cancelamento);
    }
}

internal static class ValidacaoContratos
{
    internal static async Task ExigirAtivosAsync(IContratos contratos, IReadOnlyCollection<Guid> escolhidos, CancellationToken cancelamento)
    {
        if (escolhidos.Count == 0)
        {
            throw new RegraNegocioException("Escolha pelo menos um contrato.");
        }

        var ativos = (await contratos.ListarAtivosAsync(cancelamento)).Select(c => c.Id).ToHashSet();
        if (!escolhidos.All(ativos.Contains))
        {
            throw new RegraNegocioException("Contrato inexistente ou inativo.");
        }
    }

    internal static async Task<string> NumerosAsync(IContratos contratos, IEnumerable<Guid> ids, CancellationToken cancelamento)
    {
        var numeros = (await contratos.ListarAtivosAsync(cancelamento)).ToDictionary(c => c.Id, c => c.Numero);
        return string.Join(", ", ids.Select(id => numeros.GetValueOrDefault(id, id.ToString())).Order());
    }
}
