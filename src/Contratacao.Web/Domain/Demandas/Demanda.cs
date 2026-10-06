using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Domain.Demandas;

/// <summary>
/// Demanda de contratação e sua máquina de estados (seções 6–7, 24 e Apêndice A.1).
/// Toda mudança passa por um método desta classe, que valida etapa, status e permissão do ator
/// e grava o histórico. Os campos do formulário (seção 8.1) só mudam no envio e na correção pelo Solicitante.
/// </summary>
internal sealed class Demanda
{
    private const int TamanhoMaximoTexto = 1000;
    private const int TamanhoMaximoLink = 2048;

    private readonly List<EtapaDemanda> _etapas = [];
    private readonly List<SolicitacaoCorrecao> _correcoes = [];
    private readonly List<HistoricoDemanda> _historico = [];
    private readonly List<DemandaRac> _racs = [];
    private readonly List<HistoricoAlteracao> _alteracoes = [];

    private Demanda() { } // EF Core

    internal Guid Id { get; private set; }
    internal string Numero { get; private set; } = string.Empty;
    internal Guid UsuarioSolicitanteId { get; private set; }

    /// <summary>Contrato definido pelo corredor (RN13).</summary>
    internal Guid ContratoId { get; private set; }

    /// <summary>Contratada do contrato da demanda (revisão de 05/10/2026).</summary>
    internal Guid ContratadaId { get; private set; }

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

    // Campos do formulário (seção 8.1).
    internal string? AreaSolicitante { get; private set; }
    internal Guid TipoDemandaId { get; private set; }
    internal Guid GerenteExecutivoId { get; private set; }
    internal string LocalidadeVaga { get; private set; } = string.Empty;
    internal Guid CorredorId { get; private set; }
    internal Guid ModeloTrabalhoId { get; private set; }
    internal short QuantidadeSolicitada { get; private set; }
    internal string DescricaoAtividades { get; private set; } = string.Empty;
    internal string? Formacao { get; private set; }
    internal bool Temporaria { get; private set; }
    internal short? PeriodoTemporarioMeses { get; private set; }
    internal Guid ItemQqpId { get; private set; }
    internal bool Notebook { get; private set; }
    internal bool SegundaTela { get; private set; }
    internal bool Celular { get; private set; }
    internal bool ExigeCnh { get; private set; }
    internal string? CategoriaCnh { get; private set; }
    internal Guid OrdemServicoId { get; private set; }
    internal string ColetorCusto { get; private set; } = string.Empty;
    internal string ResponsavelEfetivoNome { get; private set; } = string.Empty;
    internal string ResponsavelEfetivoEmail { get; private set; } = string.Empty;
    internal string FiscalEfetivoNome { get; private set; } = string.Empty;
    internal string FiscalEfetivoEmail { get; private set; } = string.Empty;
    internal string? Observacoes { get; private set; }

    // Custo (RN12): cópias dos valores vigentes no envio, ou na correção que o recalculou.
    internal decimal PisoSalarialQqp { get; private set; }
    internal decimal PrecoUnitarioQqp { get; private set; }
    internal decimal ValorEquipamentosPorPessoa { get; private set; }
    internal decimal CustoTotal { get; private set; }

    internal IReadOnlyList<EtapaDemanda> Etapas => _etapas;
    internal IReadOnlyList<SolicitacaoCorrecao> Correcoes => _correcoes;
    internal IReadOnlyList<HistoricoDemanda> Historico => _historico;
    internal IReadOnlyList<DemandaRac> Racs => _racs;

    /// <summary>Alterações de campo feitas nas correções (HistoricoAlteracao).</summary>
    internal IReadOnlyList<HistoricoAlteracao> Alteracoes => _alteracoes;

    internal bool Cancelada => Status == StatusDemanda.Cancelado;
    internal bool Concluida => Status == StatusDemanda.Concluido;

    /// <summary>A devolução ainda não corrigida, se houver.</summary>
    internal SolicitacaoCorrecao? CorrecaoPendente => _correcoes.SingleOrDefault(c => c.Pendente);

    /// <summary>Os campos do formulário como estão gravados.</summary>
    internal DadosSolicitacao Dados => new()
    {
        AreaSolicitante = AreaSolicitante,
        TipoDemandaId = TipoDemandaId,
        GerenteExecutivoId = GerenteExecutivoId,
        LocalidadeVaga = LocalidadeVaga,
        CorredorId = CorredorId,
        ModeloTrabalhoId = ModeloTrabalhoId,
        QuantidadeSolicitada = QuantidadeSolicitada,
        DescricaoAtividades = DescricaoAtividades,
        Formacao = Formacao,
        Temporaria = Temporaria,
        PeriodoTemporarioMeses = PeriodoTemporarioMeses,
        ItemQqpId = ItemQqpId,
        Notebook = Notebook,
        SegundaTela = SegundaTela,
        Celular = Celular,
        ExigeCnh = ExigeCnh,
        CategoriaCnh = CategoriaCnh,
        Racs = _racs.Select(r => r.RacId).ToHashSet(),
        OrdemServicoId = OrdemServicoId,
        ColetorCusto = ColetorCusto,
        ResponsavelEfetivoNome = ResponsavelEfetivoNome,
        ResponsavelEfetivoEmail = ResponsavelEfetivoEmail,
        FiscalEfetivoNome = FiscalEfetivoNome,
        FiscalEfetivoEmail = FiscalEfetivoEmail,
        Observacoes = Observacoes,
    };

    /// <summary>A passagem ainda aberta; a ordem da lista não é garantida ao carregar do banco.</summary>
    private EtapaDemanda PassagemAtual => _etapas.Single(e => e.Aberta);

    private bool AguardandoCorrecao
        => Status == StatusDemanda.AguardandoCorrecao && Etapa is Etapa.ValidacaoGestor or Etapa.ValidacaoSesi;

    /// <summary>
    /// Envio (UC02): a demanda passa a existir no sistema já em Validação do Gestor / Em análise, com os campos
    /// obrigatórios conferidos, o contrato do corredor (RN13), o custo (RN12) e o De acordo VP-2 anexado.
    /// A etapa Solicitação é registrada concluída.
    /// </summary>
    internal static Demanda Enviar(
        string numero,
        Ator solicitante,
        DadosSolicitacao dados,
        ReferenciasSolicitacao referencias,
        bool possuiDeAcordoVp2,
        IRelogio relogio)
    {
        Exigir(solicitante.Eh(Perfil.Solicitante), "Só um Solicitante ativo envia demanda.");
        Exigir(NumeroDemanda.EhValido(numero), "Número da demanda fora do formato AAAA-NNNNNN.");
        var conferidos = dados.Validar();
        ExigirReferencias(conferidos, referencias);
        Exigir(possuiDeAcordoVp2, "Anexe o De acordo VP-2: sem ele a demanda não pode ser enviada.");

        var agora = relogio.AgoraUtc;
        var demanda = new Demanda
        {
            Numero = numero,
            UsuarioSolicitanteId = solicitante.Id,
            ContratoId = referencias.Contrato.Id,
            ContratadaId = referencias.Contrato.ContratadaId,
            DataCriacao = agora,
            DataEnvio = agora,
            Etapa = Etapa.ValidacaoGestor,
            Status = StatusDemanda.EmAnalise,
        };
        demanda.Aplicar(conferidos);
        demanda.CalcularCusto(referencias);

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

        // Após nova aprovação (correção contratual), o Gestor da demanda passa a ser o da aprovação mais recente.
        GestorId = gestor.Id;
        PassagemAtual.DefinirResponsavel(gestor.Id);

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
    /// Correção pelo Solicitante (UC06): grava os campos corrigidos, cada alteração no HistoricoAlteracao, e
    /// recalcula o contrato pelo corredor (RN13). Se mudar o item QQP, a quantidade ou os equipamentos, o custo
    /// inteiro é recalculado com os valores atuais (Cliente). Devolvida pelo Gestor: volta à Validação do Gestor.
    /// Devolvida pelo SESI: volta ao SESI, salvo inconsistência contratual ou mudança de contrato, que voltam
    /// ao Gestor (RN02a).
    /// </summary>
    internal void Corrigir(Ator solicitante, DadosSolicitacao dados, ReferenciasSolicitacao referencias, IRelogio relogio)
    {
        Exigir(EhSolicitanteDaDemanda(solicitante), "Só o Solicitante que criou a demanda pode corrigi-la.");
        Exigir(AguardandoCorrecao, "A demanda não está aguardando correção.");
        var conferidos = dados.Validar();
        ExigirReferencias(conferidos, referencias);

        var agora = relogio.AgoraUtc;
        var correcao = _correcoes.Single(c => c.Pendente);
        correcao.Resolver(agora);

        var anteriores = Dados;
        var valoresAnteriores = anteriores.Campos().ToDictionary(c => c.Campo, c => c.Valor);
        foreach (var (campo, valor) in conferidos.Campos())
        {
            RegistrarAlteracao(solicitante, campo, valoresAnteriores[campo], valor, agora);
        }

        Aplicar(conferidos);

        var contratoMudou = referencias.Contrato.Id != ContratoId;
        RegistrarAlteracao(solicitante, nameof(ContratoId), ContratoId.ToString(), referencias.Contrato.Id.ToString(), agora);
        ContratoId = referencias.Contrato.Id;
        ContratadaId = referencias.Contrato.ContratadaId;

        if (anteriores.MudaCusto(conferidos))
        {
            var (piso, preco, equipamentos, total) = (PisoSalarialQqp, PrecoUnitarioQqp, ValorEquipamentosPorPessoa, CustoTotal);
            CalcularCusto(referencias);
            RegistrarAlteracao(solicitante, nameof(PisoSalarialQqp), DadosSolicitacao.Texto(piso), DadosSolicitacao.Texto(PisoSalarialQqp), agora);
            RegistrarAlteracao(solicitante, nameof(PrecoUnitarioQqp), DadosSolicitacao.Texto(preco), DadosSolicitacao.Texto(PrecoUnitarioQqp), agora);
            RegistrarAlteracao(solicitante, nameof(ValorEquipamentosPorPessoa),
                DadosSolicitacao.Texto(equipamentos), DadosSolicitacao.Texto(ValorEquipamentosPorPessoa), agora);
            RegistrarAlteracao(solicitante, nameof(CustoTotal), DadosSolicitacao.Texto(total), DadosSolicitacao.Texto(CustoTotal), agora);
        }

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
        PassagemAtual.DefinirResponsavel(sesi.Id);
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
        // O link fica na Vaga; a observação do histórico tem no máximo 1000 caracteres e o link, 2048.
        Vaga = new Vaga(linkExterno.Trim(), agora, sesi.Id);
        _historico.Add(new HistoricoDemanda(EventoDemanda.VagaAberta, sesi, agora, Etapa, Status, Etapa, Status, null));
    }

    /// <summary>Início das entrevistas (UC09): exige vaga com link registrada.</summary>
    internal void IniciarEntrevistas(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.Recrutamento, StatusDemanda.EmAndamento);
        Exigir(Vaga is not null, "Registre a vaga com o link antes de iniciar as entrevistas.");

        // Quem conclui a etapa fica como responsável por ela (registrar responsáveis e datas de cada etapa).
        PassagemAtual.DefinirResponsavel(sesi.Id);
        MudarPara(Etapa.Entrevistas, StatusDemanda.EmAndamento, StatusDemanda.Concluido,
            EventoDemanda.EntrevistasIniciadas, sesi, relogio.AgoraUtc, null);
    }

    /// <summary>Início dos exames médicos (UC10): exige a etapa Entrevistas.</summary>
    internal void IniciarExames(Ator sesi, IRelogio relogio)
    {
        ExigirSesiDoContrato(sesi);
        ExigirSituacao(Etapa.Entrevistas, StatusDemanda.EmAndamento);

        PassagemAtual.DefinirResponsavel(sesi.Id);
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
        PassagemAtual.DefinirResponsavel(sesi.Id);
        MudarPara(Etapa.Contratacao, StatusDemanda.Concluido, StatusDemanda.Concluido,
            EventoDemanda.ContratacaoFinalizada, sesi, agora, null);
        PassagemAtual.DefinirResponsavel(sesi.Id);
        PassagemAtual.Encerrar(StatusDemanda.Concluido, agora);
    }

    /// <summary>
    /// Cancelamento (UC18, RN10): Gestor do contrato ou Admin, com justificativa. A demanda fica Cancelado
    /// na etapa em que estava e o SLA é encerrado. O SESI não cancela.
    /// </summary>
    internal void Cancelar(Ator ator, string justificativa, IRelogio relogio)
    {
        Exigir(EhGestorDoContrato(ator) || ator.Eh(Perfil.Admin), "Só um Gestor do contrato ou o Admin cancela a demanda.");
        Exigir(!Concluida, "Demanda com contratação concluída não pode ser cancelada.");
        Exigir(!Cancelada, "A demanda já está cancelada.");
        ExigirTexto(justificativa, "A justificativa do cancelamento é obrigatória.");

        var agora = relogio.AgoraUtc;
        var (etapaAnterior, statusAnterior) = (Etapa, Status);

        MotivoCancelamento = justificativa.Trim();
        Status = StatusDemanda.Cancelado;
        PassagemAtual.Encerrar(StatusDemanda.Cancelado, agora);
        _historico.Add(new HistoricoDemanda(
            EventoDemanda.Cancelada, ator, agora, etapaAnterior, statusAnterior, Etapa, Status, MotivoCancelamento));
    }

    /// <summary>
    /// Anexos depois do envio: só o Solicitante da demanda, e só enquanto ela está devolvida a ele para correção
    /// (Cliente, revisão de 06/10/2026).
    /// </summary>
    internal bool AceitaAnexoDe(Ator ator) => EhSolicitanteDaDemanda(ator) && AguardandoCorrecao;

    /// <summary>Etapas para onde o Admin pode levar a demanda numa transição forçada (UC20).</summary>
    internal static readonly IReadOnlyList<Etapa> EtapasDeTransicaoForcada = [Etapa.Recrutamento, Etapa.Entrevistas, Etapa.ExamesMedicos];

    /// <summary>
    /// UC20 — Transição forçada pelo Admin: leva a demanda, para trás ou para frente, a outra etapa entre
    /// Recrutamento e Exames Médicos (Cliente, revisão de 06/10/2026). Só depois do aceite e antes da conclusão;
    /// o SLA não muda. A passagem atual termina Concluído ao avançar e Em andamento ao voltar (foi interrompida).
    /// </summary>
    internal void ForcarTransicao(Ator admin, Etapa destino, string justificativa, IRelogio relogio)
    {
        Exigir(admin.Eh(Perfil.Admin), "Só o Admin executa operações excepcionais.");
        ExigirTexto(justificativa, "A justificativa da operação excepcional é obrigatória.");
        Exigir(Status == StatusDemanda.EmAndamento && EtapasDeTransicaoForcada.Contains(Etapa),
            "A transição forçada só vale para demandas em andamento entre Recrutamento e Exames Médicos.");
        Exigir(EtapasDeTransicaoForcada.Contains(destino), "Escolha uma etapa entre Recrutamento e Exames Médicos.");
        Exigir(destino != Etapa, "A demanda já está nessa etapa.");

        var statusFinal = destino > Etapa ? StatusDemanda.Concluido : StatusDemanda.EmAndamento;
        MudarPara(destino, StatusDemanda.EmAndamento, statusFinal, EventoDemanda.TransicaoForcada, admin, relogio.AgoraUtc,
            justificativa.Trim());
    }

    /// <summary>
    /// UC20 — Alteração excepcional de data pelo Admin, a única forma de mudar uma data congelada. Datas
    /// alteráveis (Cliente, revisão de 06/10/2026): início e conclusão de cada etapa, abertura da vaga e início do
    /// SLA, que recalcula a data limite com o mesmo prazo. Cada mudança vai para o histórico de alterações com a
    /// justificativa; devolve o campo e os valores anterior e novo, para o log de auditoria.
    /// </summary>
    internal (string Campo, string? Anterior, string Novo) AlterarDataExcepcional(
        Ator admin,
        DataAlteravel data,
        Guid? passagemId,
        DateTime novaUtc,
        string justificativa,
        IRelogio relogio,
        ICalendarioSla calendario)
    {
        Exigir(admin.Eh(Perfil.Admin), "Só o Admin executa operações excepcionais.");
        ExigirTexto(justificativa, "A justificativa da operação excepcional é obrigatória.");
        Exigir(novaUtc.Kind == DateTimeKind.Utc, "A data precisa estar em UTC.");

        var agora = relogio.AgoraUtc;
        Exigir(novaUtc <= agora, "A nova data não pode estar no futuro.");
        Exigir(novaUtc >= DataCriacao, "A nova data não pode ser anterior ao envio da demanda.");

        string campo;
        DateTime? anterior;
        switch (data)
        {
            case DataAlteravel.InicioEtapa or DataAlteravel.ConclusaoEtapa:
                var passagem = _etapas.SingleOrDefault(e => e.Id == passagemId)
                    ?? throw new RegraNegocioException("Escolha uma etapa desta demanda.");
                if (data == DataAlteravel.InicioEtapa)
                {
                    (campo, anterior) = ($"{passagem.Etapa}.DataInicio", passagem.DataInicio);
                    passagem.CorrigirDatas(novaUtc, passagem.DataConclusao);
                }
                else
                {
                    Exigir(!passagem.Aberta, "A etapa ainda está em curso e não tem data de conclusão.");
                    (campo, anterior) = ($"{passagem.Etapa}.DataConclusao", passagem.DataConclusao);
                    passagem.CorrigirDatas(passagem.DataInicio, novaUtc);
                    if (passagem.Etapa == Etapa.Contratacao)
                    {
                        DataFinalizacao = novaUtc;
                    }
                }

                break;

            case DataAlteravel.AberturaVaga:
                Exigir(Vaga is not null, "A demanda ainda não tem vaga registrada.");
                (campo, anterior) = ("Vaga.DataAbertura", Vaga!.DataAbertura);
                Vaga.CorrigirDataAbertura(novaUtc);
                break;

            case DataAlteravel.InicioSla:
                Exigir(Sla is not null, "O SLA desta demanda ainda não começou.");
                (campo, anterior) = ("Sla.Inicio", Sla!.InicioUtc);
                var limiteAnterior = Sla.DataLimite;
                Sla = Sla.Iniciar(novaUtc, Sla.PrazoDias, calendario);
                RegistrarAlteracao(admin, "Sla.DataLimite", limiteAnterior.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    Sla.DataLimite.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), agora, justificativa.Trim());
                break;

            default:
                throw new RegraNegocioException("Escolha a data a alterar.");
        }

        var (textoAnterior, textoNovo) = (anterior is { } a ? DataUtc(a) : null, DataUtc(novaUtc));
        RegistrarAlteracao(admin, campo, textoAnterior, textoNovo, agora, justificativa.Trim());
        return (campo, textoAnterior, textoNovo);
    }

    /// <summary>
    /// As ações de validação, correção e do processo SESI que este ator pode executar agora, para a tela mostrar
    /// só os botões permitidos. Os métodos de cada ação exigem as mesmas condições.
    /// </summary>
    internal IReadOnlySet<AcaoDemanda> AcoesDisponiveis(Ator ator)
    {
        var acoes = new HashSet<AcaoDemanda>();
        if (EhGestorDoContrato(ator) && Em(Etapa.ValidacaoGestor, StatusDemanda.EmAnalise))
        {
            acoes.Add(AcaoDemanda.Aprovar);
            acoes.Add(AcaoDemanda.DevolverPeloGestor);
        }

        if (EhSesiDoContrato(ator) && Em(Etapa.ValidacaoSesi, StatusDemanda.AguardandoResponsavel))
        {
            acoes.Add(AcaoDemanda.Aceitar);
            acoes.Add(AcaoDemanda.DevolverPeloSesi);
        }

        if (EhSolicitanteDaDemanda(ator) && AguardandoCorrecao)
        {
            acoes.Add(AcaoDemanda.Corrigir);
        }

        // UC18: o Gestor do contrato cancela qualquer demanda ainda não concluída nem cancelada.
        if (EhGestorDoContrato(ator) && !Concluida && !Cancelada)
        {
            acoes.Add(AcaoDemanda.Cancelar);
        }

        // Processo SESI (UC08–11): sempre a próxima da sequência, sem pular nem voltar.
        if (EhSesiDoContrato(ator) && Status == StatusDemanda.EmAndamento)
        {
            var proxima = Etapa switch
            {
                Etapa.Recrutamento when Vaga is null => AcaoDemanda.RegistrarVaga,
                Etapa.Recrutamento => AcaoDemanda.IniciarEntrevistas,
                Etapa.Entrevistas => AcaoDemanda.IniciarExames,
                Etapa.ExamesMedicos => AcaoDemanda.Finalizar,
                _ => (AcaoDemanda?)null,
            };
            if (proxima is { } acao)
            {
                acoes.Add(acao);
            }
        }

        return acoes;
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

        PassagemAtual.Encerrar(statusFinalDaEtapaAtual, agora);
        _etapas.Add(EtapaDemanda.Iniciar(novaEtapa, novoStatus, agora));
        Etapa = novaEtapa;
        Status = novoStatus;

        _historico.Add(new HistoricoDemanda(evento, ator, agora, etapaAnterior, statusAnterior, Etapa, Status, observacao));
    }

    private void MudarStatus(StatusDemanda novoStatus, EventoDemanda evento, Ator ator, DateTime agora, string? observacao)
    {
        var statusAnterior = Status;

        PassagemAtual.AtualizarStatus(novoStatus);
        Status = novoStatus;

        _historico.Add(new HistoricoDemanda(evento, ator, agora, Etapa, statusAnterior, Etapa, Status, observacao));
    }

    private void Aplicar(DadosSolicitacao dados)
    {
        AreaSolicitante = dados.AreaSolicitante;
        TipoDemandaId = dados.TipoDemandaId;
        GerenteExecutivoId = dados.GerenteExecutivoId;
        LocalidadeVaga = dados.LocalidadeVaga;
        CorredorId = dados.CorredorId;
        ModeloTrabalhoId = dados.ModeloTrabalhoId;
        QuantidadeSolicitada = (short)dados.QuantidadeSolicitada;
        DescricaoAtividades = dados.DescricaoAtividades;
        Formacao = dados.Formacao;
        Temporaria = dados.Temporaria;
        PeriodoTemporarioMeses = (short?)dados.PeriodoTemporarioMeses;
        ItemQqpId = dados.ItemQqpId;
        Notebook = dados.Notebook;
        SegundaTela = dados.SegundaTela;
        Celular = dados.Celular;
        ExigeCnh = dados.ExigeCnh;
        CategoriaCnh = dados.CategoriaCnh;
        OrdemServicoId = dados.OrdemServicoId;
        ColetorCusto = dados.ColetorCusto;
        ResponsavelEfetivoNome = dados.ResponsavelEfetivoNome;
        ResponsavelEfetivoEmail = dados.ResponsavelEfetivoEmail;
        FiscalEfetivoNome = dados.FiscalEfetivoNome;
        FiscalEfetivoEmail = dados.FiscalEfetivoEmail;
        Observacoes = dados.Observacoes;

        // Edição direta da tabela de ligação (seção 8.1); a mudança fica no HistoricoAlteracao.
        _racs.RemoveAll(r => !dados.Racs.Contains(r.RacId));
        _racs.AddRange(dados.Racs.Where(id => _racs.All(r => r.RacId != id)).Select(id => new DemandaRac(id)));
    }

    /// <summary>RN12 com os valores atuais dos catálogos.</summary>
    private void CalcularCusto(ReferenciasSolicitacao referencias)
    {
        PisoSalarialQqp = referencias.Qqp.PisoSalarial;
        PrecoUnitarioQqp = referencias.Qqp.PrecoUnitario;
        ValorEquipamentosPorPessoa = CustoDemanda.EquipamentosPorPessoa(Notebook, SegundaTela, Celular, referencias.Equipamentos);
        CustoTotal = CustoDemanda.Total(QuantidadeSolicitada, PrecoUnitarioQqp, ValorEquipamentosPorPessoa);
    }

    private void RegistrarAlteracao(Ator ator, string campo, string? anterior, string? novo, DateTime agora, string? justificativa = null)
    {
        if (anterior != novo)
        {
            _alteracoes.Add(new HistoricoAlteracao(Id, ator, campo, anterior, novo, justificativa, agora));
        }
    }

    private static string DataUtc(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);

    private bool Em(Etapa etapa, StatusDemanda status) => Etapa == etapa && Status == status;

    private bool EhSolicitanteDaDemanda(Ator ator) => ator.Eh(Perfil.Solicitante) && ator.Id == UsuarioSolicitanteId;

    private bool EhGestorDoContrato(Ator ator) => ator.Eh(Perfil.Gestor) && ator.AtuaNoContrato(ContratoId);

    // Qualquer Funcionário SESI ativo do contrato executa as ações do SESI (Cliente);
    // o responsável SESI é a referência, não o único autorizado.
    private bool EhSesiDoContrato(Ator ator) => ator.Eh(Perfil.FuncionarioSesi) && ator.AtuaNoContrato(ContratoId);

    private void ExigirSituacao(Etapa etapa, StatusDemanda status)
        => Exigir(Em(etapa, status), $"Ação não permitida: a demanda está em {Etapa} / {Status}.");

    private void ExigirGestorDoContrato(Ator ator)
        => Exigir(EhGestorDoContrato(ator), "Só um Gestor ativo vinculado ao contrato da demanda pode executar esta ação.");

    private void ExigirSesiDoContrato(Ator ator)
        => Exigir(EhSesiDoContrato(ator), "Só um Funcionário SESI ativo do contrato da demanda pode executar esta ação.");

    /// <summary>
    /// Os catálogos lidos correspondem aos campos escolhidos: o corredor é o do formulário e está ativo,
    /// o contrato é o desse corredor (RN13) e está ativo, a OS é desse contrato e está ativa, o gerente executivo
    /// está ativo e atende o corredor (Cliente), e o preço é o do item QQP escolhido.
    /// </summary>
    private static void ExigirReferencias(DadosSolicitacao dados, ReferenciasSolicitacao referencias)
    {
        Exigir(referencias.Corredor.Id == dados.CorredorId && referencias.Corredor.Ativo, "Escolha um corredor ativo.");
        Exigir(referencias.Contrato.Id == referencias.Corredor.ContratoId && referencias.Contrato.Ativo,
            "O corredor escolhido não tem contrato ativo.");
        Exigir(referencias.Os.Id == dados.OrdemServicoId && referencias.Os.Ativo, "Escolha uma OS ativa.");
        Exigir(referencias.Os.ContratoId == referencias.Contrato.Id, "A OS escolhida não é do contrato do corredor.");
        Exigir(referencias.Gerente.Id == dados.GerenteExecutivoId && referencias.Gerente.Ativo, "Escolha um gerente executivo ativo.");
        Exigir(referencias.Gerente.AtendeCorredor(referencias.Corredor.Id), "O gerente executivo escolhido não atende o corredor.");
        Exigir(referencias.Qqp.ItemQqpId == dados.ItemQqpId, "O preço não corresponde ao item QQP escolhido.");
    }

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

/// <summary>Ações oferecidas na tela da demanda.</summary>
internal enum AcaoDemanda
{
    Aprovar = 1,
    DevolverPeloGestor = 2,
    Aceitar = 3,
    DevolverPeloSesi = 4,
    Corrigir = 5,
    RegistrarVaga = 6,
    IniciarEntrevistas = 7,
    IniciarExames = 8,
    Finalizar = 9,
    Cancelar = 10,
}

/// <summary>Datas que o Admin altera por operação excepcional (UC20; Cliente, revisão de 06/10/2026).</summary>
internal enum DataAlteravel
{
    InicioEtapa = 1,
    ConclusaoEtapa = 2,
    AberturaVaga = 3,
    InicioSla = 4,
}
