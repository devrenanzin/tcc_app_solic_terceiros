using System.Globalization;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Web;

/// <summary>Formatos de exibição: datas no horário de Brasília e nomes dos perfis.</summary>
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

    internal static string Perfil(Perfil perfil) => perfil switch
    {
        Domain.Usuarios.Perfil.Admin => "Admin",
        Domain.Usuarios.Perfil.Gestor => "Gestor do Contrato",
        Domain.Usuarios.Perfil.Solicitante => "Solicitante",
        Domain.Usuarios.Perfil.FuncionarioSesi => "Funcionário SESI",
        _ => perfil.ToString(),
    };
}
