using System.Globalization;
using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Parametros;

/// <summary>Parâmetros alterados pelo Admin, como o prazo padrão do SLA (RN07).</summary>
internal sealed class ParametroSistema
{
    internal const string PrazoSlaDias = "PrazoSlaDias";

    private ParametroSistema() { } // EF Core

    internal string Chave { get; private set; } = string.Empty;
    internal string Valor { get; private set; } = string.Empty;
    internal DateTime DataAlteracao { get; private set; }
    internal Guid? AlteradoPorId { get; private set; }

    internal int ValorInteiro()
        => int.TryParse(Valor, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
            ? numero
            : throw new RegraNegocioException($"O parâmetro {Chave} não é um número inteiro.");
}

/// <summary>Último sequencial usado em cada ano, para o número AAAA-NNNNNN. Usado na Etapa 4.</summary>
internal sealed class SequenciaNumeroDemanda
{
    private SequenciaNumeroDemanda() { } // EF Core

    internal SequenciaNumeroDemanda(short ano) => Ano = ano;

    internal short Ano { get; private set; }
    internal int UltimoNumero { get; private set; }
}
