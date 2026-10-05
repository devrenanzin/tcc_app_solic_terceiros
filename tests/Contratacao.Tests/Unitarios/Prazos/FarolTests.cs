using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Prazos;

namespace Contratacao.Tests.Unitarios.Prazos;

/// <summary>Cada faixa da tabela do farol (seções 9–11).</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class FarolTests
{
    private static readonly ICalendarioSla Calendario = new CalendarioBrasilia();

    // Limite em 16/10/2026.
    private static readonly Sla SlaExemplo = Sla.Iniciar(new DateTime(2026, 9, 1, 13, 15, 0, DateTimeKind.Utc), 45, Calendario);

    [Theory]
    [InlineData(30, "Verde")]
    [InlineData(11, "Verde")]
    [InlineData(10, "Amarelo")]
    [InlineData(6, "Amarelo")]
    [InlineData(5, "Laranja")]
    [InlineData(1, "Laranja")]
    [InlineData(0, "Laranja")]
    [InlineData(-1, "Vermelho")]
    [InlineData(-20, "Vermelho")]
    public void Farol_segue_os_dias_restantes(int diasRestantes, string esperado)
    {
        // A cor vem como texto porque Farol é internal e não pode aparecer na assinatura de um teste público.
        var hoje = SlaExemplo.DataLimite.AddDays(-diasRestantes);

        Assert.Equal(Enum.Parse<Farol>(esperado), RegraFarol.Calcular(SlaExemplo, false, null, hoje));
    }

    [Fact]
    public void Sla_nao_iniciado_fica_cinza()
        => Assert.Equal(Farol.Cinza, RegraFarol.Calcular(null, false, null, new DateOnly(2026, 9, 1)));

    [Fact]
    public void Cancelada_fica_cinza_mesmo_vencida()
        => Assert.Equal(Farol.Cinza, RegraFarol.Calcular(SlaExemplo, true, null, new DateOnly(2026, 12, 1)));

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public void Finalizada_ate_o_dia_limite_fica_verde_para_sempre(int diasAntesDoLimite)
    {
        var finalizacao = SlaExemplo.DataLimite.AddDays(diasAntesDoLimite);

        Assert.Equal(Farol.Verde, RegraFarol.Calcular(SlaExemplo, false, finalizacao, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Finalizada_depois_do_dia_limite_fica_vermelha()
    {
        var finalizacao = SlaExemplo.DataLimite.AddDays(1);

        Assert.Equal(Farol.Vermelho, RegraFarol.Calcular(SlaExemplo, false, finalizacao, finalizacao));
    }

    [Fact]
    public void Farol_da_demanda_usa_o_relogio_e_o_fuso_de_brasilia()
    {
        var c = new Cenario();
        var demanda = c.Enviada();
        Assert.Equal(Farol.Cinza, demanda.ObterFarol(c.Relogio, c.Calendario));

        demanda.Aprovar(c.GestorNorte, Cenario.PrazoPadrao, c.Relogio, c.Calendario);
        Assert.Equal(Farol.Verde, demanda.ObterFarol(c.Relogio, c.Calendario));

        // 16/10 22:00 em Brasília (17/10 01:00 UTC): dia limite, laranja.
        c.Relogio.Definir(new DateTime(2026, 10, 17, 1, 0, 0));
        Assert.Equal(Farol.Laranja, demanda.ObterFarol(c.Relogio, c.Calendario));

        // 17/10 00:00 em Brasília: vencida.
        c.Relogio.Definir(new DateTime(2026, 10, 17, 3, 0, 0));
        Assert.Equal(Farol.Vermelho, demanda.ObterFarol(c.Relogio, c.Calendario));
    }
}
