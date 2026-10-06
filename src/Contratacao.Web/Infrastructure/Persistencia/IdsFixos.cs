using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>
/// Ids fixos dos registros da carga inicial. Perfil, Etapa e Status são enums no domínio e
/// viram chaves uniqueidentifier nas tabelas de catálogo por meio destes mapas.
/// </summary>
internal static class IdsFixos
{
    internal static readonly IReadOnlyDictionary<Perfil, Guid> Perfis = new Dictionary<Perfil, Guid>
    {
        [Perfil.Admin] = Guid.Parse("00000001-0000-0000-0000-000000000001"),
        [Perfil.Gestor] = Guid.Parse("00000001-0000-0000-0000-000000000002"),
        [Perfil.Solicitante] = Guid.Parse("00000001-0000-0000-0000-000000000003"),
        [Perfil.FuncionarioSesi] = Guid.Parse("00000001-0000-0000-0000-000000000004"),
    };

    internal static readonly IReadOnlyDictionary<Etapa, Guid> Etapas = new Dictionary<Etapa, Guid>
    {
        [Etapa.Solicitacao] = Guid.Parse("00000002-0000-0000-0000-000000000001"),
        [Etapa.ValidacaoGestor] = Guid.Parse("00000002-0000-0000-0000-000000000002"),
        [Etapa.ValidacaoSesi] = Guid.Parse("00000002-0000-0000-0000-000000000003"),
        [Etapa.Recrutamento] = Guid.Parse("00000002-0000-0000-0000-000000000004"),
        [Etapa.Entrevistas] = Guid.Parse("00000002-0000-0000-0000-000000000005"),
        [Etapa.ExamesMedicos] = Guid.Parse("00000002-0000-0000-0000-000000000006"),
        [Etapa.Contratacao] = Guid.Parse("00000002-0000-0000-0000-000000000007"),
    };

    internal static readonly IReadOnlyDictionary<StatusDemanda, Guid> Status = new Dictionary<StatusDemanda, Guid>
    {
        [StatusDemanda.EmAnalise] = Guid.Parse("00000003-0000-0000-0000-000000000001"),
        [StatusDemanda.AguardandoCorrecao] = Guid.Parse("00000003-0000-0000-0000-000000000002"),
        [StatusDemanda.AguardandoResponsavel] = Guid.Parse("00000003-0000-0000-0000-000000000003"),
        [StatusDemanda.EmAndamento] = Guid.Parse("00000003-0000-0000-0000-000000000004"),
        [StatusDemanda.Concluido] = Guid.Parse("00000003-0000-0000-0000-000000000005"),
        [StatusDemanda.Cancelado] = Guid.Parse("00000003-0000-0000-0000-000000000006"),
    };

    internal static readonly Guid ModeloPresencial = Guid.Parse("00000004-0000-0000-0000-000000000001");
    internal static readonly Guid ModeloHibrido = Guid.Parse("00000004-0000-0000-0000-000000000002");
    internal static readonly Guid ModeloRemoto = Guid.Parse("00000004-0000-0000-0000-000000000003");

    internal static readonly Guid TipoNovaContratacao = Guid.Parse("00000005-0000-0000-0000-000000000001");

    internal static readonly Guid EquipamentoNotebook = Guid.Parse("00000007-0000-0000-0000-000000000001");
    internal static readonly Guid EquipamentoSegundaTela = Guid.Parse("00000007-0000-0000-0000-000000000002");
    internal static readonly Guid EquipamentoCelular = Guid.Parse("00000007-0000-0000-0000-000000000003");

    internal static readonly Guid ContratadaSesi = Guid.Parse("00000008-0000-0000-0000-000000000001");

    internal static readonly Guid ContratoNorte = Guid.Parse("00000009-0000-0000-0000-000000000001");
    internal static readonly Guid ContratoSudeste = Guid.Parse("00000009-0000-0000-0000-000000000002");

    internal static readonly Guid RegiaoNorte = Guid.Parse("0000000a-0000-0000-0000-000000000001");
    internal static readonly Guid RegiaoSudeste = Guid.Parse("0000000a-0000-0000-0000-000000000002");

    internal static readonly Guid CorredorNorte = Guid.Parse("0000000b-0000-0000-0000-000000000001");
    internal static readonly Guid CorredorPelotizacaoNorte = Guid.Parse("0000000b-0000-0000-0000-000000000002");
    internal static readonly Guid CorredorIntegradoNorte = Guid.Parse("0000000b-0000-0000-0000-000000000003");
    internal static readonly Guid CorredorSudeste = Guid.Parse("0000000b-0000-0000-0000-000000000004");
    internal static readonly Guid CorredorSul = Guid.Parse("0000000b-0000-0000-0000-000000000005");
    internal static readonly Guid CorredorPelotizacaoSudeste = Guid.Parse("0000000b-0000-0000-0000-000000000006");
    internal static readonly Guid CorredorIntegradoSudeste = Guid.Parse("0000000b-0000-0000-0000-000000000007");

    /// <summary>OS da carga inicial (Cliente): 01 a 10 no contrato Norte e 11 a 20 no Sudeste; o id termina no número.</summary>
    internal static Guid OrdemServico(int numero) => Guid.Parse($"0000000c-0000-0000-0000-{numero:D12}");
}
