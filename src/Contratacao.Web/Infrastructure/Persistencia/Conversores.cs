using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>Converte um enum do domínio no Guid fixo da linha correspondente do catálogo.</summary>
internal abstract class ConversorEnumGuid<TEnum>(IReadOnlyDictionary<TEnum, Guid> mapa)
    : ValueConverter<TEnum, Guid>(
        valor => mapa[valor],
        id => mapa.Single(par => par.Value == id).Key)
    where TEnum : struct, Enum;

internal sealed class ConversorPerfil() : ConversorEnumGuid<Perfil>(IdsFixos.Perfis);

internal sealed class ConversorEtapa() : ConversorEnumGuid<Etapa>(IdsFixos.Etapas);

internal sealed class ConversorStatus() : ConversorEnumGuid<StatusDemanda>(IdsFixos.Status);

/// <summary>
/// Datas são gravadas em UTC (RNF09). O SQL Server devolve datetime2 sem fuso; a leitura marca o valor
/// como UTC para o domínio não confundi-lo com horário local.
/// </summary>
internal sealed class ConversorDataUtc() : ValueConverter<DateTime, DateTime>(
    valor => valor.Kind == DateTimeKind.Utc ? valor : valor.ToUniversalTime(),
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

/// <summary>Origem da devolução gravada como no script: 'Gestor' ou 'SESI'.</summary>
internal sealed class ConversorOrigemCorrecao() : ValueConverter<OrigemCorrecao, string>(
    valor => valor == OrigemCorrecao.Sesi ? "SESI" : "Gestor",
    texto => texto == "SESI" ? OrigemCorrecao.Sesi : OrigemCorrecao.Gestor);
