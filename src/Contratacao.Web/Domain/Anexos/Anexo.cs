using Contratacao.Web.Domain.Demandas;

namespace Contratacao.Web.Domain.Anexos;

internal enum CategoriaAnexo
{
    Geral = 1,
    DeAcordoVP2 = 2,
}

/// <summary>
/// Metadados de um arquivo anexado (seção 18). O arquivo fica numa pasta do servidor com nome gerado
/// pelo sistema (Identificador) e nunca é apagado. As regras de upload entram na Etapa 6.
/// </summary>
internal sealed class Anexo
{
    private Anexo() { } // EF Core

    internal Anexo(
        Guid demandaId,
        Etapa etapa,
        Guid usuarioUploadId,
        string nomeArquivo,
        string tipoArquivo,
        long tamanho,
        string identificador,
        CategoriaAnexo categoria,
        DateTime dataUploadUtc)
    {
        DemandaId = demandaId;
        Etapa = etapa;
        UsuarioUploadId = usuarioUploadId;
        NomeArquivo = nomeArquivo;
        TipoArquivo = tipoArquivo;
        Tamanho = tamanho;
        Identificador = identificador;
        Categoria = categoria;
        DataUpload = dataUploadUtc;
    }

    internal Guid Id { get; private set; }
    internal Guid DemandaId { get; private set; }

    /// <summary>Etapa em que o arquivo foi enviado.</summary>
    internal Etapa Etapa { get; private set; }

    internal Guid UsuarioUploadId { get; private set; }
    internal string NomeArquivo { get; private set; } = string.Empty;
    internal string TipoArquivo { get; private set; } = string.Empty;
    internal long Tamanho { get; private set; }
    internal string Identificador { get; private set; } = string.Empty;
    internal CategoriaAnexo Categoria { get; private set; }
    internal DateTime DataUpload { get; private set; }
}
