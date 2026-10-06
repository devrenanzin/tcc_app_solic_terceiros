using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Notificacoes;

/// <summary>Os avisos que o sistema dá (seção 29, mais os pedidos do cliente na revisão de 06/10/2026).</summary>
internal enum TipoNotificacao
{
    NovaDemanda = 1,
    DemandaAprovada = 2,
    CorrecaoSolicitada = 3,
    AceitaPelaGerenciadora = 4,
    ProximaDoVencimento = 5,
    PrazoVencido = 6,
    ContratacaoFinalizada = 7,
    DemandaCancelada = 8,
}

/// <summary>Grupos de pessoas que recebem avisos de uma demanda.</summary>
internal enum Destinatario
{
    Solicitante = 1,
    GestoresDoContrato = 2,
    GerenciadoraDoContrato = 3,
    GestorDaDemanda = 4,
}

/// <summary>
/// Quem recebe cada aviso (seção 29): nova demanda → Gestores do contrato; aprovada → Gerenciadora do contrato;
/// correção solicitada → Solicitante; aceite → Gestor da demanda; prazo próximo e vencido → todos os Gestores e
/// Funcionários da Gerenciadora do contrato (Cliente). O Solicitante também é avisado da aprovação, da finalização e
/// do cancelamento (Cliente, revisão de 06/10/2026).
/// </summary>
internal static class RegrasNotificacao
{
    internal static IReadOnlyList<Destinatario> Para(TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.NovaDemanda => [Destinatario.GestoresDoContrato],
        TipoNotificacao.DemandaAprovada => [Destinatario.GerenciadoraDoContrato, Destinatario.Solicitante],
        TipoNotificacao.CorrecaoSolicitada => [Destinatario.Solicitante],
        TipoNotificacao.AceitaPelaGerenciadora => [Destinatario.GestorDaDemanda],
        TipoNotificacao.ProximaDoVencimento or TipoNotificacao.PrazoVencido => [Destinatario.GestoresDoContrato, Destinatario.GerenciadoraDoContrato],
        TipoNotificacao.ContratacaoFinalizada or TipoNotificacao.DemandaCancelada => [Destinatario.Solicitante],
        _ => [],
    };

    /// <summary>Avisos de prazo saem uma vez cada, por demanda (Cliente).</summary>
    internal static bool UmaVezPorDemanda(TipoNotificacao tipo) => tipo is TipoNotificacao.ProximaDoVencimento or TipoNotificacao.PrazoVencido;
}

/// <summary>Aviso dentro do sistema para um usuário (sininho do menu). Nunca é excluído; só é marcado como lido.</summary>
internal sealed class Notificacao
{
    internal const int TamanhoMaximoMensagem = 300;

    private Notificacao() { } // EF Core

    internal Guid Id { get; private set; }
    internal Guid UsuarioId { get; private set; }
    internal Guid? DemandaId { get; private set; }
    internal TipoNotificacao Tipo { get; private set; }
    internal string Mensagem { get; private set; } = string.Empty;
    internal DateTime DataCriacao { get; private set; }
    internal DateTime? DataLeitura { get; private set; }
    internal bool Lida => DataLeitura is not null;

    internal static Notificacao Criar(Guid usuarioId, Guid? demandaId, TipoNotificacao tipo, string mensagem, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(mensagem) || mensagem.Length > TamanhoMaximoMensagem)
        {
            throw new RegraNegocioException($"A mensagem do aviso é obrigatória e tem no máximo {TamanhoMaximoMensagem} caracteres.");
        }

        return new Notificacao { UsuarioId = usuarioId, DemandaId = demandaId, Tipo = tipo, Mensagem = mensagem, DataCriacao = agoraUtc };
    }

    /// <summary>Só o dono marca como lida; marcar de novo não muda a data da primeira leitura.</summary>
    internal void MarcarLida(Ator dono, DateTime agoraUtc)
    {
        if (!dono.Ativo || dono.Id != UsuarioId)
        {
            throw new RegraNegocioException("Este aviso é de outra pessoa.");
        }

        DataLeitura ??= agoraUtc;
    }
}
