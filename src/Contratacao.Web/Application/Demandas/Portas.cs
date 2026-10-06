using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;

namespace Contratacao.Web.Application.Demandas;

// Portas usadas pelos casos de uso da demanda; implementadas em Infrastructure.

internal interface IDemandas
{
    /// <summary>A demanda com etapas, devoluções, histórico, vaga, Riscos e alterações, para ser alterada.</summary>
    Task<Demanda?> ObterAsync(Guid id, CancellationToken cancelamento);

    void Adicionar(Demanda demanda);

    /// <summary>Lista das demandas que o filtro de visibilidade deixa ver, mais recentes primeiro.</summary>
    Task<IReadOnlyList<ResumoDemanda>> ListarAsync(FiltroVisibilidade filtro, CancellationToken cancelamento);

    /// <summary>
    /// Próximo sequencial do ano para o número AAAA-NNNNNN (SequenciaNumeroDemanda), sem repetição mesmo com
    /// envios simultâneos. Chamado dentro da transação do envio.
    /// </summary>
    Task<int> ProximoSequencialAsync(int ano, CancellationToken cancelamento);
}

internal interface ICatalogosDemanda
{
    /// <summary>Opções das listas do formulário (só os itens ativos).</summary>
    Task<CatalogosFormulario> FormularioAsync(CancellationToken cancelamento);

    /// <summary>Itens ativos do QQP, para as listas em cascata.</summary>
    Task<IReadOnlyList<OpcaoQqp>> ItensQqpAsync(CancellationToken cancelamento);

    Task<PrecoQqp?> PrecoQqpAsync(Guid itemQqpId, CancellationToken cancelamento);

    Task<ValoresEquipamentos> EquipamentosAsync(CancellationToken cancelamento);

    /// <summary>Corredor e o seu contrato (RN13), a OS e o gerente escolhidos, como estão hoje; nulo se algum não existir.</summary>
    Task<ReferenciasSolicitacao?> ReferenciasAsync(
        DadosSolicitacao dados, PrecoQqp qqp, ValoresEquipamentos equipamentos, CancellationToken cancelamento);

    /// <summary>Mensagens para os catálogos escolhidos que não existem ou estão inativos (tipo, modelo, Riscos).</summary>
    Task<IReadOnlyList<string>> ConferirEscolhasAsync(DadosSolicitacao dados, CancellationToken cancelamento);

    /// <summary>Nomes legíveis dos catálogos e usuários citados pela demanda, para a tela.</summary>
    Task<DescricoesDemanda> DescreverAsync(Demanda demanda, CancellationToken cancelamento);

    /// <summary>Todos os gerentes, com os corredores que atendem.</summary>
    Task<IReadOnlyList<GerenteExecutivo>> GerentesExecutivosAsync(CancellationToken cancelamento);

    Task<GerenteExecutivo?> GerenteExecutivoAsync(Guid id, CancellationToken cancelamento);

    void Adicionar(GerenteExecutivo gerente);

    /// <summary>Todas as OS, ativas ou não, com o número do contrato.</summary>
    Task<IReadOnlyList<OpcaoOs>> OrdensServicoAsync(CancellationToken cancelamento);

    Task<OrdemServico?> OrdemServicoAsync(Guid id, CancellationToken cancelamento);

    Task<bool> OrdemServicoExisteAsync(Guid contratoId, string numero, CancellationToken cancelamento);

    void Adicionar(OrdemServico ordemServico);
}

internal interface IAnexos
{
    void Adicionar(Anexo anexo);

    Task<IReadOnlyList<Anexo>> DaDemandaAsync(Guid demandaId, CancellationToken cancelamento);

    Task<Anexo?> ObterAsync(Guid id, CancellationToken cancelamento);
}

/// <summary>Pasta do servidor onde ficam os arquivos anexados; nada é apagado (seção 18).</summary>
internal interface IArmazenamentoArquivos
{
    /// <summary>Grava o arquivo com nome gerado pelo sistema e devolve o identificador (caminho relativo).</summary>
    Task<string> GuardarAsync(Stream conteudo, string extensao, CancellationToken cancelamento);

    Stream Abrir(string identificador);
}

internal interface IParametros
{
    /// <summary>Prazo padrão do SLA vigente (RN07).</summary>
    Task<int> PrazoSlaDiasAsync(CancellationToken cancelamento);
}

/// <summary>Arquivo escolhido na tela, ainda não conferido.</summary>
internal sealed record ArquivoRecebido(string Nome, long Tamanho, Func<Stream> Abrir);

internal sealed record Opcao(Guid Id, string Nome);

internal sealed record OpcaoCorredor(Guid Id, string Nome, string Regiao, Guid ContratoId, string Contrato);

internal sealed record OpcaoRac(Guid Id, string Codigo, string Nome);

internal sealed record OpcaoGerente(Guid Id, string Nome, IReadOnlyList<Guid> Corredores);

internal sealed record OpcaoOs(Guid Id, string Numero, Guid ContratoId, string Contrato, bool Ativo);

internal sealed record CatalogosFormulario(
    IReadOnlyList<Opcao> TiposDemanda,
    IReadOnlyList<OpcaoGerente> GerentesExecutivos,
    IReadOnlyList<OpcaoCorredor> Corredores,
    IReadOnlyList<Opcao> ModelosTrabalho,
    IReadOnlyList<OpcaoRac> Racs,
    IReadOnlyList<OpcaoOs> OrdensServico,
    ValoresEquipamentos Equipamentos);

/// <summary>Um item do QQP com os nomes das cinco listas em cascata.</summary>
internal sealed record OpcaoQqp(
    Guid Id,
    int Codigo,
    string Regiao,
    string Funcao,
    string? Classificacao,
    string Nivel,
    short OrdemNivel,
    short CargaHoraria,
    decimal PisoSalarial,
    decimal PrecoUnitario);

/// <summary>Linha das listas e da tela de acompanhamento (seções 21–22).</summary>
internal sealed record ResumoDemanda(
    Guid Id,
    string Numero,
    Guid SolicitanteId,
    string Solicitante,
    Guid ContratoId,
    string Contrato,
    Guid ContratadaId,
    string Contratada,
    Guid? GestorId,
    string? Gestor,
    Guid? ResponsavelSesiId,
    string? ResponsavelSesi,
    Etapa Etapa,
    StatusDemanda Status,
    DateTime DataEnvio,
    string LocalidadeVaga,
    string Funcao,
    short Quantidade,
    decimal CustoTotal,
    Sla? Sla,
    DateTime? DataFinalizacao)
{
    internal Farol Farol { get; init; }

    internal PosicaoDemanda Posicao => new(Etapa, Status, Farol, ContratoId);
}

/// <summary>Nomes para mostrar a demanda; os dicionários traduzem os ids gravados no histórico de alterações.</summary>
internal sealed record DescricoesDemanda(
    IReadOnlyDictionary<Guid, string> Usuarios,
    IReadOnlyDictionary<Guid, string> Contratos,
    IReadOnlyDictionary<Guid, string> Contratadas,
    IReadOnlyDictionary<Guid, string> TiposDemanda,
    IReadOnlyDictionary<Guid, string> GerentesExecutivos,
    IReadOnlyDictionary<Guid, string> Corredores,
    IReadOnlyDictionary<Guid, string> ModelosTrabalho,
    IReadOnlyDictionary<Guid, string> ItensQqp,
    IReadOnlyDictionary<Guid, string> Racs,
    IReadOnlyDictionary<Guid, string> OrdensServico)
{
    internal static string Nome(IReadOnlyDictionary<Guid, string> nomes, Guid? id)
        => id is { } valor && nomes.TryGetValue(valor, out var nome) ? nome : "—";
}

/// <summary>Tudo o que a tela da demanda mostra, já filtrado pela visibilidade do ator.</summary>
internal sealed record DetalheDemanda(
    Demanda Demanda,
    DescricoesDemanda Descricoes,
    IReadOnlyList<Anexo> Anexos,
    Farol Farol,
    IReadOnlySet<AcaoDemanda> Acoes);
