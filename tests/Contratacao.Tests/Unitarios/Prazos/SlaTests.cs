using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Prazos;

namespace Contratacao.Tests.Unitarios.Prazos;

/// <summary>SLA (RN03, RN04, RN05, RN07, RN08).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class SlaTests
{
    private readonly ICalendarioSla _calendario = new CalendarioBrasilia();

    [Fact]
    public void Exemplo_do_documento_01_09_mais_45_dias_vence_em_16_10()
    {
        var sla = Sla.Iniciar(Utc(2026, 9, 1, 13, 15), 45, _calendario);

        Assert.Equal(new DateOnly(2026, 10, 16), sla.DataLimite);
        Assert.Equal(new DateOnly(2026, 9, 1), sla.DataInicioLocal);
    }

    [Fact]
    public void Exemplo_do_documento_em_16_09_tem_15_dias_decorridos_e_30_restantes()
    {
        var sla = Sla.Iniciar(Utc(2026, 9, 1, 13, 15), 45, _calendario);
        var hoje = new DateOnly(2026, 9, 16);

        Assert.Equal(15, sla.DiasDecorridos(hoje));
        Assert.Equal(30, sla.DiasRestantes(hoje));
    }

    [Fact]
    public void Contagem_usa_a_data_de_brasilia_e_nao_a_data_utc()
    {
        // 02/09 01:30 UTC = 01/09 22:30 em Brasília.
        var sla = Sla.Iniciar(Utc(2026, 9, 2, 1, 30), 45, _calendario);

        Assert.Equal(new DateOnly(2026, 9, 1), sla.DataInicioLocal);
        Assert.Equal(new DateOnly(2026, 10, 16), sla.DataLimite);
    }

    [Fact]
    public void Dia_limite_tem_zero_restantes_e_o_dia_seguinte_fica_negativo()
    {
        var sla = Sla.Iniciar(Utc(2026, 9, 1, 13, 15), 45, _calendario);

        Assert.Equal(0, sla.DiasRestantes(new DateOnly(2026, 10, 16)));
        Assert.Equal(-1, sla.DiasRestantes(new DateOnly(2026, 10, 17)));
    }

    [Fact]
    public void Vence_as_23_59_59_de_brasilia_do_dia_limite()
    {
        var sla = Sla.Iniciar(Utc(2026, 9, 1, 13, 15), 45, _calendario);

        // 16/10 23:59:59 em Brasília = 17/10 02:59:59 UTC: ainda no prazo.
        var ultimoInstante = _calendario.DataLocal(new DateTime(2026, 10, 17, 2, 59, 59, DateTimeKind.Utc));
        // 17/10 00:00 em Brasília = 17/10 03:00 UTC: vencido.
        var vencido = _calendario.DataLocal(Utc(2026, 10, 17, 3, 0));

        Assert.Equal(0, sla.DiasRestantes(ultimoInstante));
        Assert.Equal(-1, sla.DiasRestantes(vencido));
    }

    [Fact]
    public void Prazo_gravado_e_o_vigente_no_inicio()
    {
        var sla = Sla.Iniciar(Utc(2026, 9, 1, 13, 15), 30, _calendario);

        Assert.Equal(30, sla.PrazoDias);
        Assert.Equal(new DateOnly(2026, 10, 1), sla.DataLimite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Prazo_precisa_ser_positivo(int prazo)
        => Assert.Throws<RegraNegocioException>(() => Sla.Iniciar(Utc(2026, 9, 1, 13, 15), prazo, _calendario));

    [Fact]
    public void Mudanca_do_prazo_padrao_nao_altera_demanda_ja_aprovada()
    {
        var c = new Cenario();
        var antiga = c.Aprovada();

        var nova = Contratacao.Web.Domain.Demandas.Demanda.Enviar("2026-000124", c.Solicitante, Cenario.ContratoNorte, c.Relogio);
        nova.Aprovar(c.GestorNorte, 30, c.Relogio, c.Calendario);

        Assert.Equal(45, antiga.Sla!.PrazoDias);
        Assert.Equal(30, nova.Sla!.PrazoDias);
    }

    [Fact]
    public void Calendario_recusa_instante_que_nao_esta_em_utc()
        => Assert.Throws<ArgumentException>(() => _calendario.DataLocal(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Local)));

    private static DateTime Utc(int ano, int mes, int dia, int hora, int minuto)
        => new(ano, mes, dia, hora, minuto, 0, DateTimeKind.Utc);
}
