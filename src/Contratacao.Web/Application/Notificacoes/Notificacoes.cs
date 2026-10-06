using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Notificacoes;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Notificacoes;

internal interface INotificacoes
{
    void Adicionar(Notificacao notificacao);

    /// <summary>Já existe aviso deste tipo para a demanda (avisos de prazo saem uma vez só).</summary>
    Task<bool> ExisteAsync(Guid demandaId, TipoNotificacao tipo, CancellationToken cancelamento);

    Task<IReadOnlyList<Notificacao>> DoUsuarioAsync(Guid usuarioId, int limite, CancellationToken cancelamento);

    Task<int> NaoLidasAsync(Guid usuarioId, CancellationToken cancelamento);

    Task<Notificacao?> ObterAsync(Guid id, CancellationToken cancelamento);

    Task<IReadOnlyList<Notificacao>> NaoLidasDoUsuarioAsync(Guid usuarioId, CancellationToken cancelamento);
}

/// <summary>O que o aviso precisa saber da demanda.</summary>
internal sealed record AlvoNotificacao(Guid DemandaId, string Numero, Guid ContratoId, Guid SolicitanteId, Guid? GestorId)
{
    internal static AlvoNotificacao De(Demanda d) => new(d.Id, d.Numero, d.ContratoId, d.UsuarioSolicitanteId, d.GestorId);
}

/// <summary>
/// Gera os avisos de uma demanda para quem deve recebê-los (RegrasNotificacao). Chamado pelos casos de uso antes de
/// gravar, na mesma unidade de trabalho: o aviso só existe se a ação foi gravada.
/// </summary>
internal sealed class Notificador(INotificacoes notificacoes, IUsuarios usuarios, IRelogio relogio)
{
    internal Task RegistrarAsync(Demanda demanda, TipoNotificacao tipo, CancellationToken cancelamento)
        => RegistrarAsync(AlvoNotificacao.De(demanda), tipo, cancelamento);

    internal async Task RegistrarAsync(AlvoNotificacao alvo, TipoNotificacao tipo, CancellationToken cancelamento)
    {
        var pessoas = new HashSet<Guid>();
        foreach (var destinatario in RegrasNotificacao.Para(tipo))
        {
            switch (destinatario)
            {
                case Destinatario.Solicitante:
                    pessoas.Add(alvo.SolicitanteId);
                    break;
                case Destinatario.GestorDaDemanda when alvo.GestorId is { } gestor:
                    pessoas.Add(gestor);
                    break;
                case Destinatario.GestoresDoContrato:
                    pessoas.UnionWith(await usuarios.AtivosDoContratoAsync(Perfil.Gestor, alvo.ContratoId, cancelamento));
                    break;
                case Destinatario.GerenciadoraDoContrato:
                    pessoas.UnionWith(await usuarios.AtivosDoContratoAsync(Perfil.FuncionarioSesi, alvo.ContratoId, cancelamento));
                    break;
            }
        }

        var mensagem = Mensagem(tipo, alvo.Numero);
        foreach (var pessoa in pessoas)
        {
            notificacoes.Adicionar(Notificacao.Criar(pessoa, alvo.DemandaId, tipo, mensagem, relogio.AgoraUtc));
        }
    }

    internal static string Mensagem(TipoNotificacao tipo, string numero) => tipo switch
    {
        TipoNotificacao.NovaDemanda => $"Nova demanda {numero} aguardando a validação do Gestor.",
        TipoNotificacao.DemandaAprovada => $"Demanda {numero} aprovada pelo Gestor; aguarda o aceite da Gerenciadora.",
        TipoNotificacao.CorrecaoSolicitada => $"Demanda {numero} devolvida para correção.",
        TipoNotificacao.AceitaPelaGerenciadora => $"Demanda {numero} aceita pela Gerenciadora; o recrutamento começou.",
        TipoNotificacao.ProximaDoVencimento => $"O prazo da demanda {numero} está perto de vencer.",
        TipoNotificacao.PrazoVencido => $"O prazo da demanda {numero} venceu.",
        TipoNotificacao.ContratacaoFinalizada => $"Contratação da demanda {numero} finalizada.",
        TipoNotificacao.DemandaCancelada => $"Demanda {numero} cancelada.",
        _ => $"Demanda {numero} atualizada.",
    };
}

/// <summary>
/// Avisos de prazo (seção 29): uma vez quando a demanda fica próxima do vencimento (farol amarelo ou laranja) e uma
/// vez quando vence (Cliente). Executado periodicamente pelo sistema.
/// </summary>
internal sealed class VerificarPrazos(IDemandas demandas, INotificacoes notificacoes, Notificador notificador, IUnidadeDeTrabalho unidade,
    IRelogio relogio, ICalendarioSla calendario)
{
    internal async Task<int> ExecutarAsync(CancellationToken cancelamento)
    {
        var hoje = calendario.DataLocal(relogio.AgoraUtc);
        var avisos = 0;
        foreach (var d in await demandas.ListarAsync(FiltroVisibilidade.Todas, cancelamento))
        {
            if (d.Sla is null || d.Status is StatusDemanda.Concluido or StatusDemanda.Cancelado)
            {
                continue;
            }

            TipoNotificacao? tipo = RegraFarol.Calcular(d.Sla, false, null, hoje) switch
            {
                Farol.Amarelo or Farol.Laranja => TipoNotificacao.ProximaDoVencimento,
                Farol.Vermelho => TipoNotificacao.PrazoVencido,
                _ => null,
            };
            if (tipo is not { } aviso || await notificacoes.ExisteAsync(d.Id, aviso, cancelamento))
            {
                continue;
            }

            await notificador.RegistrarAsync(new AlvoNotificacao(d.Id, d.Numero, d.ContratoId, d.SolicitanteId, d.GestorId), aviso, cancelamento);
            avisos++;
        }

        if (avisos > 0)
        {
            await unidade.SalvarAsync(cancelamento);
        }

        return avisos;
    }
}

/// <summary>Sininho do menu: os avisos do próprio usuário.</summary>
internal sealed class ConsultarNotificacoes(INotificacoes notificacoes, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal const int Limite = 100;

    internal Task<IReadOnlyList<Notificacao>> ListarAsync(Ator ator, CancellationToken cancelamento)
        => notificacoes.DoUsuarioAsync(ator.Id, Limite, cancelamento);

    internal Task<int> NaoLidasAsync(Guid usuarioId, CancellationToken cancelamento) => notificacoes.NaoLidasAsync(usuarioId, cancelamento);

    /// <summary>Marca o aviso como lido e devolve a demanda a abrir.</summary>
    internal async Task<Guid?> AbrirAsync(Ator ator, Guid notificacaoId, CancellationToken cancelamento)
    {
        var notificacao = await notificacoes.ObterAsync(notificacaoId, cancelamento)
            ?? throw new RegraNegocioException("Aviso não encontrado.");
        notificacao.MarcarLida(ator, relogio.AgoraUtc);
        await unidade.SalvarAsync(cancelamento);
        return notificacao.DemandaId;
    }

    internal async Task MarcarTodasLidasAsync(Ator ator, CancellationToken cancelamento)
    {
        foreach (var notificacao in await notificacoes.NaoLidasDoUsuarioAsync(ator.Id, cancelamento))
        {
            notificacao.MarcarLida(ator, relogio.AgoraUtc);
        }

        await unidade.SalvarAsync(cancelamento);
    }
}
