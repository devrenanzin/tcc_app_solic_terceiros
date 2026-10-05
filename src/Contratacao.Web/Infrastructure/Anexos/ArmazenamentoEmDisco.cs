using System.Globalization;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Comum;
using Microsoft.Extensions.Options;

namespace Contratacao.Web.Infrastructure.Anexos;

internal sealed class OpcoesAnexos
{
    internal const string Secao = "Anexos";

    /// <summary>Pasta dos arquivos; relativa à pasta da aplicação quando não for absoluta.</summary>
    public string Pasta { get; set; } = Path.Combine("App_Data", "anexos");
}

/// <summary>
/// Arquivos anexados numa pasta do servidor (seção 18). O nome no disco é gerado pelo sistema, nunca o
/// original, e nada é sobrescrito nem apagado.
/// </summary>
internal sealed class ArmazenamentoEmDisco(IOptions<OpcoesAnexos> opcoes, IHostEnvironment ambiente, IRelogio relogio) : IArmazenamentoArquivos
{
    private readonly string _raiz = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, opcoes.Value.Pasta));

    public async Task<string> GuardarAsync(Stream conteudo, string extensao, CancellationToken cancelamento)
    {
        var agora = relogio.AgoraUtc;
        var identificador = string.Join('/',
            agora.Year.ToString("D4", CultureInfo.InvariantCulture),
            agora.Month.ToString("D2", CultureInfo.InvariantCulture),
            $"{Guid.NewGuid():N}{extensao}");

        var caminho = Caminho(identificador);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);

        await using var destino = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await conteudo.CopyToAsync(destino, cancelamento);
        return identificador;
    }

    public Stream Abrir(string identificador)
        => new FileStream(Caminho(identificador), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    private string Caminho(string identificador)
    {
        var caminho = Path.GetFullPath(Path.Combine(_raiz, identificador));
        if (!caminho.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Identificador de anexo fora da pasta de anexos.");
        }

        return caminho;
    }
}
