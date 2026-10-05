using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Apoio;

/// <summary>Atores, catálogos e demandas fictícios para os testes do domínio.</summary>
internal sealed class Cenario
{
    internal const int PrazoPadrao = 45;
    internal const string Numero = "2026-000123";

    internal static readonly Guid ContratadaSesi = Guid.Parse("00000000-0000-0000-0000-00000000a001");
    internal static readonly Contrato ContratoNorte = Contrato.Criar(Guid.Parse("00000000-0000-0000-0000-00000000c001"), ContratadaSesi, "5900125082");
    internal static readonly Contrato ContratoSudeste = Contrato.Criar(Guid.Parse("00000000-0000-0000-0000-00000000c002"), ContratadaSesi, "5900118506");

    internal static readonly Guid RegiaoNorte = Guid.Parse("00000000-0000-0000-0000-00000000e001");
    internal static readonly Guid RegiaoSudeste = Guid.Parse("00000000-0000-0000-0000-00000000e002");
    internal static readonly Corredor CorredorNorte = Corredor.Criar(Guid.Parse("00000000-0000-0000-0000-00000000b001"), "Norte", RegiaoNorte, ContratoNorte.Id);
    internal static readonly Corredor CorredorSudeste = Corredor.Criar(Guid.Parse("00000000-0000-0000-0000-00000000b002"), "Sudeste", RegiaoSudeste, ContratoSudeste.Id);

    // Valores confirmados pelo cliente (RN12) e dois itens QQP fictícios.
    internal static readonly ValoresEquipamentos Equipamentos = new(444.35m, 53.93m, 118.64m);
    internal static readonly PrecoQqp ItemAnalista = new(Guid.Parse("00000000-0000-0000-0000-00000000f001"), 5_000.00m, 10_000.00m);
    internal static readonly PrecoQqp ItemEngenheiro = new(Guid.Parse("00000000-0000-0000-0000-00000000f002"), 8_000.00m, 16_000.00m);

    internal static readonly Guid Rac01 = Guid.Parse("00000000-0000-0000-0000-00000000d001");
    internal static readonly Guid Rac02 = Guid.Parse("00000000-0000-0000-0000-00000000d002");

    // 01/09/2026 08:45 em Brasília (UTC-3).
    internal RelogioFixo Relogio { get; } = new(new DateTime(2026, 9, 1, 11, 45, 0, DateTimeKind.Utc));
    internal ICalendarioSla Calendario { get; } = new CalendarioBrasilia();

    internal Ator Solicitante { get; } = Novo(Perfil.Solicitante);
    internal Ator OutroSolicitante { get; } = Novo(Perfil.Solicitante);
    internal Ator GestorNorte { get; } = Novo(Perfil.Gestor, ContratoNorte);
    internal Ator GestorSudeste { get; } = Novo(Perfil.Gestor, ContratoSudeste);
    internal Ator GestorDosDois { get; } = Novo(Perfil.Gestor, ContratoNorte, ContratoSudeste);
    internal Ator GestorInativo { get; } = Novo(Perfil.Gestor, ContratoNorte) with { Ativo = false };
    internal Ator SesiNorte { get; } = Novo(Perfil.FuncionarioSesi, ContratoNorte);
    internal Ator OutroSesiNorte { get; } = Novo(Perfil.FuncionarioSesi, ContratoNorte);
    internal Ator SesiSudeste { get; } = Novo(Perfil.FuncionarioSesi, ContratoSudeste);
    internal Ator Admin { get; } = Novo(Perfil.Admin);

    internal static Ator Novo(Perfil perfil, params Contrato[] contratos)
        => new(Guid.NewGuid(), perfil, true, contratos.Select(c => c.Id).ToHashSet(), "10.0.0.1");

    /// <summary>Formulário completo, no corredor Norte, com notebook, 2 vagas e uma RAC.</summary>
    internal static DadosSolicitacao Dados() => new()
    {
        AreaSolicitante = "Engenharia de Manutenção",
        TipoDemandaId = Guid.Parse("00000000-0000-0000-0000-000000000a01"),
        GerenteExecutivoId = Guid.Parse("00000000-0000-0000-0000-000000000a02"),
        LocalidadeVaga = "Vitória",
        CorredorId = CorredorNorte.Id,
        ModeloTrabalhoId = Guid.Parse("00000000-0000-0000-0000-000000000a03"),
        QuantidadeSolicitada = 2,
        DescricaoAtividades = "Apoio à manutenção preventiva.",
        Temporaria = false,
        ItemQqpId = ItemAnalista.ItemQqpId,
        Notebook = true,
        Racs = new HashSet<Guid> { Rac01 },
        ContratoOs = "15",
        ColetorCusto = "CC-1234",
        ResponsavelEfetivoNome = "Pessoa Responsável",
        ResponsavelEfetivoEmail = "responsavel@ucl.br",
        FiscalEfetivoNome = "Pessoa Fiscal",
        FiscalEfetivoEmail = "fiscal@ucl.br",
    };

    internal static ReferenciasSolicitacao Referencias(Corredor corredor, PrecoQqp? item = null, ValoresEquipamentos? equipamentos = null)
        => new(corredor, corredor == CorredorSudeste ? ContratoSudeste : ContratoNorte, item ?? ItemAnalista, equipamentos ?? Equipamentos);

    internal Demanda Enviar(string numero, Ator? solicitante = null)
        => Demanda.Enviar(numero, solicitante ?? Solicitante, Dados(), Referencias(CorredorNorte), true, Relogio);

    /// <summary>Correção sem mudança de campos, ou com troca para o corredor Sudeste (outro contrato).</summary>
    internal void Corrigir(Demanda demanda, Ator? ator = null, bool paraSudeste = false)
        => demanda.Corrigir(
            ator ?? Solicitante,
            paraSudeste ? Dados() with { CorredorId = CorredorSudeste.Id } : Dados(),
            Referencias(paraSudeste ? CorredorSudeste : CorredorNorte),
            Relogio);

    internal Demanda Enviada() => Enviar(Numero);

    internal Demanda Aprovada()
    {
        var demanda = Enviada();
        demanda.Aprovar(GestorNorte, PrazoPadrao, Relogio, Calendario);
        return demanda;
    }

    internal Demanda EmRecrutamento()
    {
        var demanda = Aprovada();
        demanda.Aceitar(SesiNorte, Relogio);
        return demanda;
    }

    internal Demanda ComVaga()
    {
        var demanda = EmRecrutamento();
        demanda.RegistrarVaga(SesiNorte, "https://vagas.exemplo.ucl.br/123", Relogio);
        return demanda;
    }

    internal Demanda EmEntrevistas()
    {
        var demanda = ComVaga();
        demanda.IniciarEntrevistas(SesiNorte, Relogio);
        return demanda;
    }

    internal Demanda EmExames()
    {
        var demanda = EmEntrevistas();
        demanda.IniciarExames(SesiNorte, Relogio);
        return demanda;
    }

    internal Demanda Finalizada()
    {
        var demanda = EmExames();
        demanda.Finalizar(SesiNorte, Relogio);
        return demanda;
    }
}
