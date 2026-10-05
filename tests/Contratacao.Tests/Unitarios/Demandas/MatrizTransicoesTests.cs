using Contratacao.Tests.Unitarios.Apoio;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Tests.Unitarios.Demandas;

/// <summary>
/// Seção 24: cada ação em cada situação da demanda, sempre com o ator correto.
/// Só as transições da tabela são aceitas; todas as outras são recusadas.
/// </summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class MatrizTransicoesTests
{
    private static readonly Dictionary<string, Func<Cenario, Demanda>> Situacoes = new()
    {
        ["ValidacaoGestor/EmAnalise"] = c => c.Enviada(),
        ["ValidacaoGestor/AguardandoCorrecao"] = c =>
        {
            var d = c.Enviada();
            d.DevolverPeloGestor(c.GestorNorte, "Corrigir a localidade.", c.Relogio);
            return d;
        },
        ["ValidacaoSesi/AguardandoResponsavel"] = c => c.Aprovada(),
        ["ValidacaoSesi/AguardandoCorrecao"] = c =>
        {
            var d = c.Aprovada();
            d.DevolverPeloSesi(c.SesiNorte, TipoInconsistencia.Solicitante, "Descrição incompleta.", c.Relogio);
            return d;
        },
        ["Recrutamento sem vaga"] = c => c.EmRecrutamento(),
        ["Recrutamento com vaga"] = c => c.ComVaga(),
        ["Entrevistas"] = c => c.EmEntrevistas(),
        ["ExamesMedicos"] = c => c.EmExames(),
        ["Contratacao/Concluido"] = c => c.Finalizada(),
        ["Cancelada"] = c =>
        {
            var d = c.Aprovada();
            d.Cancelar(c.GestorNorte, "Vaga suspensa.", c.Relogio);
            return d;
        },
    };

    private static readonly Dictionary<string, Action<Cenario, Demanda>> Acoes = new()
    {
        ["Aprovar"] = (c, d) => d.Aprovar(c.GestorNorte, Cenario.PrazoPadrao, c.Relogio, c.Calendario),
        ["DevolverPeloGestor"] = (c, d) => d.DevolverPeloGestor(c.GestorNorte, "Motivo.", c.Relogio),
        ["DevolverPeloSesi"] = (c, d) => d.DevolverPeloSesi(c.SesiNorte, TipoInconsistencia.Solicitante, "Motivo.", c.Relogio),
        ["Corrigir"] = (c, d) => d.Corrigir(c.Solicitante, Cenario.ContratoNorte, c.Relogio),
        ["Aceitar"] = (c, d) => d.Aceitar(c.SesiNorte, c.Relogio),
        ["RegistrarVaga"] = (c, d) => d.RegistrarVaga(c.SesiNorte, "https://vagas.exemplo.ucl.br/999", c.Relogio),
        ["IniciarEntrevistas"] = (c, d) => d.IniciarEntrevistas(c.SesiNorte, c.Relogio),
        ["IniciarExames"] = (c, d) => d.IniciarExames(c.SesiNorte, c.Relogio),
        ["Finalizar"] = (c, d) => d.Finalizar(c.SesiNorte, c.Relogio),
        ["Cancelar"] = (c, d) => d.Cancelar(c.GestorNorte, "Justificativa.", c.Relogio),
    };

    private static readonly Dictionary<string, string[]> Permitidas = new()
    {
        ["ValidacaoGestor/EmAnalise"] = ["Aprovar", "DevolverPeloGestor", "Cancelar"],
        ["ValidacaoGestor/AguardandoCorrecao"] = ["Corrigir", "Cancelar"],
        ["ValidacaoSesi/AguardandoResponsavel"] = ["Aceitar", "DevolverPeloSesi", "Cancelar"],
        ["ValidacaoSesi/AguardandoCorrecao"] = ["Corrigir", "Cancelar"],
        ["Recrutamento sem vaga"] = ["RegistrarVaga", "Cancelar"],
        ["Recrutamento com vaga"] = ["IniciarEntrevistas", "Cancelar"],
        ["Entrevistas"] = ["IniciarExames", "Cancelar"],
        ["ExamesMedicos"] = ["Finalizar", "Cancelar"],
        ["Contratacao/Concluido"] = [],
        ["Cancelada"] = [],
    };

    public static TheoryData<string, string, bool> Casos()
    {
        var casos = new TheoryData<string, string, bool>();
        foreach (var situacao in Situacoes.Keys)
        {
            foreach (var acao in Acoes.Keys)
            {
                casos.Add(situacao, acao, Permitidas[situacao].Contains(acao));
            }
        }

        return casos;
    }

    [Theory]
    [MemberData(nameof(Casos))]
    public void Acao_so_e_aceita_quando_a_tabela_de_transicoes_permite(string situacao, string acao, bool permitida)
    {
        var cenario = new Cenario();
        var demanda = Situacoes[situacao](cenario);
        var historicoAntes = demanda.Historico.Count;

        var executar = () => Acoes[acao](cenario, demanda);

        if (permitida)
        {
            executar();
            Assert.Equal(historicoAntes + 1, demanda.Historico.Count);
        }
        else
        {
            Assert.Throws<RegraNegocioException>(executar);
            Assert.Equal(historicoAntes, demanda.Historico.Count);
        }
    }
}
