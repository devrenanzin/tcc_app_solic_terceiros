using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>
/// Demanda de contratação e sua máquina de estados (seções 6–7, 24 e Apêndice A.1).
/// Toda mudança passa por um método desta classe, que valida etapa, status e permissão do ator
/// e grava o histórico. Os campos do formulário (seção 8.1) entram na Etapa 4.
/// </summary>
internal sealed class Demanda
{
    private const int TamanhoMaximoTexto = 1000;
    private const int TamanhoMaximoLink = 2048;

    private readonly List<EtapaDemanda> _etapas = [];
    private readonly List<SolicitacaoCorrecao> _correcoes = [];
    private readonly List<HistoricoDemanda> _historico = [];

    private Demanda() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Numero { get; private set; } = string.Empty;
    internal Guid UsuarioSolicitanteId { get; private set; }
    internal Guid ContratoId { get; private set; }

    /// <summary>O Gestor que valida a demanda; nulo até a primeira aprovação.</summary>
    internal Guid? GestorId { get; private set; }

    /// <summary>Quem aceitou a demanda (RF38); nulo até o aceite.</summary>
    internal Guid? ResponsavelSesiId { get; private set; }

    internal Etapa Etapa { get; private set; }
    internal StatusDemanda Status { get; private set; }
    internal DateTime DataCriacao { get; private set; }
    internal DateTime DataEnvio { get; private set; }
    internal Sla? Sla { get; private set; }
    internal DateTime? DataFinalizacao { get; private set; }
    internal string? MotivoCancelamento { get; private set; }
    internal Vaga? Vaga { get; private set; }

    internal IReadOnlyList<EtapaDemanda> Etapas => _etapas;
    internal IReadOnlyList<SolicitacaoCorrecao> Correcoes => _correcoes;
    internal IReadOnlyList<HistoricoDemanda> Historico => _historico;

    internal bool Cancelada => Status == StatusDemanda.Cancelado;
    internal bool Concluida => Status == StatusDemanda.Concluido;

    /// <summary>
    /// Envio (UC02): a demanda passa a existir no sistema já em Validação do Gestor / Em análise.
    /// A etapa Solicitação é registrada concluída. Campos obrigatórios e anexo VP-2 são verificados na Etapa 4.
    /// </summary>
    internal static Demanda Enviar(string numero, Ator solicitante, Guid contratoId, IRelogio relogio)
    {
        Exigir(solicitante.Eh(Perfil.Solicitante), "Só um Solicitante ativo envia demanda.");
        Exigir(NumeroDemanda.EhValido(numero), "Número da demanda fora do formato AAAA-NNNNNN.");
        Exigir(contratoId != Guid.Empty, "A demanda precisa de um contrato.");

        var agora = relogio.AgoraUtc;
        var demanda = new Demanda
        {
            Numero = numero,
            UsuarioSolicitanteId = solicitante.Id,
            ContratoId = contratoId,
            DataCriacao = agora,
            DataEnvio = agora,
            Etapa = Etapa.ValidacaoGestor,
            Status = StatusDemanda.EmAnalise,
        };

        var solicitacao = EtapaDemanda.Iniciar(Etapa.Solicitacao, StatusDemanda.EmAnalise, agora);
        solicitacao.DefinirResponsavel(solicitante.Id);
        solicitacao.Encerrar(StatusDemanda.Concluido, agora);
        demanda._etapas.Add(solicitacao);
        demanda._etapas.Add(EtapaDemanda.Iniciar(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise, agora));

        demanda._historico.Add(new HistoricoDemanda(
            EventoDemanda.Enviada, solicitante, agora, null, null, Etapa.ValidacaoGestor, StatusDemanda.EmAnalise, null));

        return demanda;
    }

    /// <summary>Aprovação pelo Gestor do contrato (UC04). A primeira aprovação inicia o SLA (RN03, RN07).</summary>
    internal void Aprovar(Ator gestor, int prazoPadraoDias, IRelogio relogio, ICalendarioSla calendario)
    {
        ExigirGestorDoContrato(gestor);
        ExigirSituacao(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise);

        var agora = relogio.AgoraUtc;

        // PENDENTE: confirmar se, após nova aprovação, o Gestor da demanda é o da primeira ou o da última.
        // Hoje fica o da última aprovação.
        GestorId = gestor.Id;
        _etapas[^1].DefinirResponsavel(gestor.Id);

        // RN05: aprovações seguintes (após correção contratual) não reiniciam nem alteram o SLA.
        Sla ??= Sla.Iniciar(agora, prazoPadraoDias, calendario);

        MudarPara(Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel, StatusDemanda.Concluido,
            EventoDemanda.AprovadaPeloGestor, gestor, agora, null);
    }

    /// <summary>Devolução pelo Gestor ao Solicitante, com motivo (RN01, UC05).</summary>
    internal void DevolverPeloGestor(Ator gestor, string motivo, IRelogio relogio)
    {
        ExigirGestorDoContrato(gestor);
        ExigirSituacao(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise);
        ExigirTexto(motivo, "O motivo da devolução é obrigatório.");

        var agora = relogio.AgoraUtc;
        _correcoes.Add(SolicitacaoCorrecao.PeloGestor(gestor.Id, motivo.Trim(), agora));
        MudarStatus(StatusDemanda.AguardandoCorrecao, EventoDemanda.DevolvidaParaCorrecao, gestor, agora, motivo.Trim());
    }

    /// <summary>Devolução pelo SESI ao Solicitante, com motivo e tipo da inconsistência (RN02, UC05).</summary>
    internal void DevolverPeloSesi(Ator sesi, TipoInconsistencia tipo, string motivo, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel);
        Exigir(Enum.IsDefined(tipo), "Tipo de inconsistência inválido.");
        ExigirTexto(motivo, "O motivo da devolução é obrigatório.");

        var agora = relogio.AgoraUtc;
        _correcoes.Add(SolicitacaoCorrecao.PeloSesi(sesi.Id, tipo, motivo.Trim(), agora));
        MudarStatus(StatusDemanda.AguardandoCorrecao, EventoDemanda.DevolvidaParaCorrecao, sesi, agora, motivo.Trim());
    }

    /// <summary>
    /// Correção pelo Solicitante (UC06). O contrato informado é o recalculado a partir do corredor (RN13).
    /// Devolvida pelo Gestor: volta à Validação do Gestor. Devolvida pelo SESI: volta ao SESI, salvo
    /// inconsistência contratual ou mudança de contrato, que voltam ao Gestor (RN02a).
    /// </summary>
    internal void Corrigir(Ator solicitante, Guid contratoId, IRelogio relogio)
    {
        Exigir(solicitante.Eh(Perfil.Solicitante) && solicitante.Id == UsuarioSolicitanteId,
            "Só o Solicitante que criou a demanda pode corrigi-la.");
        Exigir(Status == StatusDemanda.AguardandoCorrecao && Etapa is Etapa.ValidacaoGestor or Etapa.ValidacaoSesi,
            "A demanda não está aguardando correção.");
        Exigir(contratoId != Guid.Empty, "A demanda precisa de um contrato.");

        var agora = relogio.AgoraUtc;
        var correcao = _correcoes.Last(c => c.Pendente);
        correcao.Resolver(agora);

        var contratoMudou = contratoId != ContratoId;
        ContratoId = contratoId;

        if (Etapa == Etapa.ValidacaoGestor)
        {
            MudarStatus(StatusDemanda.EmAnalise, EventoDemanda.CorrecaoEnviada, solicitante, agora, null);
        }
        else if (correcao.Tipo == TipoInconsistencia.Contratual || contratoMudou)
        {
            // A passagem pela Validação SESI termina sem conclusão: a demanda volta uma etapa.
            MudarPara(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise, StatusDemanda.AguardandoCorrecao,
                EventoDemanda.CorrecaoEnviada, solicitante, agora, null);
        }
        else
        {
            MudarStatus(StatusDemanda.AguardandoResponsavel, EventoDemanda.CorrecaoEnviada, solicitante, agora, null);
        }
    }

    /// <summary>Aceite pelo SESI (UC07): quem aceita vira o responsável SESI (RF38).</summary>
    internal void Aceitar(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel);

        ResponsavelSesiId = sesi.Id;
        _etapas[^1].DefinirResponsavel(sesi.Id);
        MudarPara(Etapa.Recrutamento, StatusDemanda.EmAndamento, StatusDemanda.Concluido,
            EventoDemanda.AceitaPeloSesi, sesi, relogio.AgoraUtc, null);
    }

    /// <summary>Registro da vaga com link externo (UC08). É um evento: etapa e status não mudam.</summary>
    internal void RegistrarVaga(Ator sesi, string linkExterno, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.Recrutamento, StatusDemanda.EmAndamento);
        Exigir(Vaga is null, "A vaga desta demanda já foi registrada.");
        Exigir(LinkValido(linkExterno), "Informe um link http ou https válido para a vaga.");

        var agora = relogio.AgoraUtc;
        Vaga = new Vaga(linkExterno.Trim(), agora, sesi.Id);
        _historico.Add(new HistoricoDemanda(EventoDemanda.VagaAberta, sesi, agora, Etapa, Status, Etapa, Status, linkExterno.Trim()));
    }

    /// <summary>Início das entrevistas (UC09): exige vaga com link registrada.</summary>
    internal void IniciarEntrevistas(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.Recrutamento, StatusDemanda.EmAndamento);
        Exigir(Vaga is not null, "Registre a vaga com o link antes de iniciar as entrevistas.");

        MudarPara(Etapa.Entrevistas, StatusDemanda.EmAndamento, StatusDemanda.Concluido,
            EventoDemanda.EntrevistasIniciadas, sesi, relogio.AgoraUtc, null);
    }

    /// <summary>Início dos exames médicos (UC10): exige a etapa Entrevistas.</summary>
    internal void IniciarExames(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.Entrevistas, StatusDemanda.EmAndamento);

        MudarPara(Etapa.ExamesMedicos, StatusDemanda.EmAndamento, StatusDemanda.Concluido,
            EventoDemanda.ExamesIniciados, sesi, relogio.AgoraUtc, null);
    }

    /// <summary>Finalização da contratação (UC11): exige a etapa Exames Médicos e encerra o SLA.</summary>
    internal void Finalizar(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.ExamesMedicos, StatusDemanda.EmAndamento);

        var agora = relogio.AgoraUtc;
        DataFinalizacao = agora;
        MudarPara(Etapa.Contratacao, StatusDemanda.Concluido, StatusDemanda.Concluido,
            EventoDemanda.ContratacaoFinalizada, sesi, agora, null);
        _etapas[^1].Encerrar(StatusDemanda.Concluido, agora);
    }

    /// <summary>
    /// Cancelamento (UC18, RN10): Gestor do contrato ou Admin, com justificativa. A demanda fica Cancelado
    /// na etapa em que estava e o SLA é encerrado. O SESI não cancela.
    /// </summary>
    internal void Cancelar(Ator ator, string justificativa, IRelogio relogio)
    {
        var gestorDoContrato = ator.Eh(Perfil.Gestor) && ator.AtuaNoContrato(ContratoId);
        Exigir(gestorDoContrato || ator.Eh(Perfil.Admin), "Só um Gestor do contrato ou o Admin cancela a demanda.");
        Exigir(!Concluida, "Demanda com contratação concluída não pode ser cancelada.");
        Exigir(!Cancelada, "A demanda já está cancelada.");
        ExigirTexto(justificativa, "A justificativa do cancelamento é obrigatória.");

        var agora = relogio.AgoraUtc;
        var (etapaAnterior, statusAnterior) = (Etapa, Status);

        MotivoCancelamento = justificativa.Trim();
        Status = StatusDemanda.Cancelado;
        _etapas[^1].Encerrar(StatusDemanda.Cancelado, agora);
        _historico.Add(new HistoricoDemanda(
            EventoDemanda.Cancelada, ator, agora, etapaAnterior, statusAnterior, Etapa, Status, MotivoCancelamento));
    }

    /// <summary>Farol da demanda hoje, no horário de Brasília (seções 9–11).</summary>
    internal Farol ObterFarol(IRelogio relogio, ICalendarioSla calendario)
    {
        DateOnly? finalizacao = DataFinalizacao is { } data ? calendario.DataLocal(data) : null;
        return RegraFarol.Calcular(Sla, Cancelada, finalizacao, calendario.DataLocal(relogio.AgoraUtc));
    }

    private void MudarPara(
        Etapa novaEtapa,
        StatusDemanda novoStatus,
        StatusDemanda statusFinalDaEtapaAtual,
        EventoDemanda evento,
        Ator ator,
        DateTime agora,
        string? observacao)
    {
        var (etapaAnterior, statusAnterior) = (Etapa, Status);

        _etapas[^1].Encerrar(statusFinalDaEtapaAtual, agora);
        _etapas.Add(EtapaDemanda.Iniciar(novaEtapa, novoStatus, agora));
        Etapa = novaEtapa;
        Status = novoStatus;

        _historico.Add(new HistoricoDemanda(evento, ator, agora, etapaAnterior, statusAnterior, Etapa, Status, observacao));
    }

    private void MudarStatus(StatusDemanda novoStatus, EventoDemanda evento, Ator ator, DateTime agora, string? observacao)
    {
        var statusAnterior = Status;

        _etapas[^1].AtualizarStatus(novoStatus);
        Status = novoStatus;

        _historico.Add(new HistoricoDemanda(evento, ator, agora, Etapa, statusAnterior, Etapa, Status, observacao));
    }

    private void ExigirSituacao(Etapa etapa, StatusDemanda status)
        => Exigir(Etapa == etapa && Status == status,
            $"Ação não permitida: a demanda está em {Etapa} / {Status}.");

    private void ExigirGestorDoContrato(Ator ator)
        => Exigir(ator.Eh(Perfil.Gestor) && ator.AtuaNoContrato(ContratoId),
            "Só um Gestor ativo vinculado ao contrato da demanda pode executar esta ação.");

    // SUPOSIÇÃO (S4): qualquer Funcionário SESI ativo do contrato executa as ações do SESI;
    // o responsável SESI é a referência, não o único autorizado.
    private void ExigirSesiDoContrato(Ator ator)
        => Exigir(ator.Eh(Perfil.FuncionarioSesi) && ator.AtuaNoContrato(ContratoId),
            "Só um Funcionário SESI ativo do contrato da demanda pode executar esta ação.");

    private static void ExigirTexto(string? texto, string mensagem)
    {
        Exigir(!string.IsNullOrWhiteSpace(texto), mensagem);
        Exigir(texto!.Trim().Length <= TamanhoMaximoTexto, $"O texto pode ter no máximo {TamanhoMaximoTexto} caracteres.");
    }

    private static bool LinkValido(string? link)
        => !string.IsNullOrWhiteSpace(link)
        && link.Trim().Length <= TamanhoMaximoLink
        && Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new RegraNegocioException(mensagem);
        }
    }
}
