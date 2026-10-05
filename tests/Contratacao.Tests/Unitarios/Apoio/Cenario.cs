using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Tests.Unitarios.Apoio;

/// <summary>Atores e demandas fictícios para os testes do domínio.</summary>
internal sealed class Cenario
{
    internal const int PrazoPadrao = 45;
    internal const string Numero = "2026-000123";

    internal static readonly Guid ContratadaSesi = Guid.Parse("00000000-0000-0000-0000-00000000a001");
    internal static readonly Contrato ContratoNorte = Contrato.Criar(Guid.Parse("00000000-0000-0000-0000-00000000c001"), ContratadaSesi, "5900125082");
    internal static readonly Contrato ContratoSudeste = Contrato.Criar(Guid.Parse("00000000-0000-0000-0000-00000000c002"), ContratadaSesi, "5900118506");

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

    internal Demanda Enviada() => Demanda.Enviar(Numero, Solicitante, ContratoNorte, Relogio);

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
