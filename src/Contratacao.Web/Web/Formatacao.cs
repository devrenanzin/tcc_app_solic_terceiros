using System.Globalization;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Prazos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Web;

/// <summary>Formatos de exibição: datas no horário de Brasília, valores em reais e nomes de perfis, etapas e status.</summary>
internal static class Formatacao
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    internal static string DataHora(DateTime? utc)
        => utc is { } valor
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(valor, DateTimeKind.Utc), Brasilia).ToString("dd/MM/yyyy HH:mm", PtBr)
            : "—";

    internal static string Data(DateTime? utc)
        => utc is { } valor
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(valor, DateTimeKind.Utc), Brasilia).ToString("dd/MM/yyyy", PtBr)
            : "—";

    internal static string Data(DateOnly? data) => data?.ToString("dd/MM/yyyy", PtBr) ?? "—";

    internal static string Reais(decimal? valor) => valor?.ToString("C2", PtBr) ?? "—";

    internal static string Numero(int valor) => valor.ToString("N0", PtBr);

    internal static string SimNao(bool valor) => valor ? "Sim" : "Não";

    internal static string Perfil(Perfil perfil) => perfil switch
    {
        Domain.Usuarios.Perfil.Admin => "Admin",
        Domain.Usuarios.Perfil.Gestor => "Gestor do Contrato",
        Domain.Usuarios.Perfil.Solicitante => "Solicitante",
        Domain.Usuarios.Perfil.FuncionarioSesi => "Funcionário SESI",
        _ => perfil.ToString(),
    };

    /// <summary>Nomes da seção 5.1.</summary>
    internal static string Etapa(Etapa etapa) => etapa switch
    {
        Domain.Demandas.Etapa.Solicitacao => "Solicitação",
        Domain.Demandas.Etapa.ValidacaoGestor => "Validação do Gestor",
        Domain.Demandas.Etapa.ValidacaoSesi => "Validação SESI",
        Domain.Demandas.Etapa.Recrutamento => "Recrutamento",
        Domain.Demandas.Etapa.Entrevistas => "Entrevistas",
        Domain.Demandas.Etapa.ExamesMedicos => "Exames Médicos",
        Domain.Demandas.Etapa.Contratacao => "Contratação",
        _ => etapa.ToString(),
    };

    /// <summary>Nomes da seção 5.2.</summary>
    internal static string Status(StatusDemanda status) => status switch
    {
        StatusDemanda.EmAnalise => "Em análise",
        StatusDemanda.AguardandoCorrecao => "Aguardando correção",
        StatusDemanda.AguardandoResponsavel => "Aguardando responsável",
        StatusDemanda.EmAndamento => "Em andamento",
        StatusDemanda.Concluido => "Concluído",
        StatusDemanda.Cancelado => "Cancelado",
        _ => status.ToString(),
    };

    /// <summary>Classe CSS do selo de status.</summary>
    internal static string ClasseStatus(StatusDemanda status) => status switch
    {
        StatusDemanda.AguardandoCorrecao => "status-correcao",
        StatusDemanda.Concluido => "status-concluido",
        StatusDemanda.Cancelado => "status-cancelado",
        _ => "status-andamento",
    };

    /// <summary>Eventos da linha do tempo (seção 5.3).</summary>
    internal static string Evento(EventoDemanda evento) => evento switch
    {
        EventoDemanda.Enviada => "Enviada",
        EventoDemanda.AprovadaPeloGestor => "Aprovada pelo Gestor",
        EventoDemanda.DevolvidaParaCorrecao => "Devolvida para correção",
        EventoDemanda.CorrecaoEnviada => "Correção enviada",
        EventoDemanda.AceitaPeloSesi => "Aceita pelo SESI",
        EventoDemanda.VagaAberta => "Vaga aberta",
        EventoDemanda.EntrevistasIniciadas => "Entrevistas iniciadas",
        EventoDemanda.ExamesIniciados => "Exames iniciados",
        EventoDemanda.ContratacaoFinalizada => "Contratação finalizada",
        EventoDemanda.Cancelada => "Cancelada",
        _ => evento.ToString(),
    };

    internal static string IconeEvento(EventoDemanda evento) => evento switch
    {
        EventoDemanda.Enviada => "bi-send",
        EventoDemanda.AprovadaPeloGestor => "bi-check2-circle",
        EventoDemanda.DevolvidaParaCorrecao => "bi-arrow-return-left",
        EventoDemanda.CorrecaoEnviada => "bi-pencil-square",
        EventoDemanda.AceitaPeloSesi => "bi-person-check",
        EventoDemanda.VagaAberta => "bi-link-45deg",
        EventoDemanda.Cancelada => "bi-x-circle",
        _ => "bi-circle",
    };

    internal static string Farol(Farol farol) => farol switch
    {
        Domain.Prazos.Farol.Cinza => "Sem prazo",
        Domain.Prazos.Farol.Verde => "No prazo",
        Domain.Prazos.Farol.Amarelo => "Atenção",
        Domain.Prazos.Farol.Laranja => "Vence logo",
        Domain.Prazos.Farol.Vermelho => "Atrasada",
        _ => farol.ToString(),
    };

    internal static string ClasseFarol(Farol farol) => $"farol-{farol.ToString().ToLowerInvariant()}";

    internal static string Tipo(TipoInconsistencia tipo) => tipo switch
    {
        TipoInconsistencia.Solicitante => "Informações do solicitante",
        TipoInconsistencia.Contratual => "Informações contratuais",
        _ => tipo.ToString(),
    };

    internal static string Tamanho(long bytes)
        => bytes >= 1024 * 1024
            ? $"{(bytes / 1024d / 1024d).ToString("0.0", PtBr)} MB"
            : $"{Math.Max(1, bytes / 1024).ToString(PtBr)} KB";
}
