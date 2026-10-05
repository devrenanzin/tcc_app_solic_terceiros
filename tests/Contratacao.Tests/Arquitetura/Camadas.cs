using System.Reflection;

namespace Contratacao.Tests.Arquitetura;

/// <summary>Nomes das camadas do diagrama de pacotes (Apêndice A.3).</summary>
internal static class Camadas
{
    internal static readonly Assembly AssemblyWeb = typeof(Program).Assembly;

    internal const string Domain = "Contratacao.Web.Domain";
    internal const string Application = "Contratacao.Web.Application";
    internal const string Infrastructure = "Contratacao.Web.Infrastructure";
    internal const string Web = "Contratacao.Web.Web";

    internal static readonly string[] Todas = [Domain, Application, Infrastructure, Web];
}
