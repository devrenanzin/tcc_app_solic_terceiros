using Contratacao.Web.Domain.Comum;

namespace Contratacao.Web.Domain.Anexos;

/// <summary>Arquivo conferido: nome original sem caminho, extensão e tipo reconhecidos pelo conteúdo.</summary>
internal sealed record ArquivoConferido(string NomeArquivo, string Extensao, string TipoArquivo, long Tamanho);

/// <summary>
/// Regras de arquivo da seção 18: imagens, PDF e e-mail, até 10 MB. O sistema confere a extensão e o
/// conteúdo do arquivo (os primeiros bytes), não só o nome.
/// </summary>
internal static class RegraArquivo
{
    internal const long TamanhoMaximo = 10 * 1024 * 1024;
    internal const int TamanhoMaximoNome = 260;

    /// <summary>Quantos bytes do início do arquivo bastam para reconhecer o conteúdo.</summary>
    internal const int BytesParaConferir = 512;

    private static readonly byte[] AssinaturaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] AssinaturaPdf = "%PDF-"u8.ToArray();

    // Arquivo .msg do Outlook: formato Compound File Binary.
    private static readonly byte[] AssinaturaMsg = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    // Imagens = .jpg, .jpeg e .png; e-mail = .eml e .msg (Cliente).
    private static readonly Dictionary<string, (string Tipo, Func<byte[], bool> Confere)> Aceitos = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ("image/jpeg", inicio => Comeca(inicio, AssinaturaJpeg)),
        [".jpeg"] = ("image/jpeg", inicio => Comeca(inicio, AssinaturaJpeg)),
        [".png"] = ("image/png", inicio => Comeca(inicio, AssinaturaPng)),
        [".pdf"] = ("application/pdf", inicio => Comeca(inicio, AssinaturaPdf)),
        [".eml"] = ("message/rfc822", EhMensagemDeEmail),
        [".msg"] = ("application/vnd.ms-outlook", inicio => Comeca(inicio, AssinaturaMsg)),
    };

    /// <summary>Extensões aceitas, para o campo de escolha de arquivo da tela.</summary>
    internal static IEnumerable<string> Extensoes => Aceitos.Keys;

    /// <param name="inicio">Os primeiros bytes do arquivo (até <see cref="BytesParaConferir"/>).</param>
    internal static ArquivoConferido Conferir(string? nomeOriginal, long tamanho, byte[] inicio)
    {
        var nome = Path.GetFileName(nomeOriginal?.Trim() ?? string.Empty);
        Exigir(nome.Length > 0, "Arquivo sem nome.");
        Exigir(nome.Length <= TamanhoMaximoNome, $"O nome do arquivo tem no máximo {TamanhoMaximoNome} caracteres.");
        Exigir(tamanho > 0, $"O arquivo {nome} está vazio.");
        Exigir(tamanho <= TamanhoMaximo, $"O arquivo {nome} passa de 10 MB.");

        var extensao = Path.GetExtension(nome).ToLowerInvariant();
        Exigir(Aceitos.TryGetValue(extensao, out var aceito),
            $"O arquivo {nome} não é imagem (.jpg, .jpeg, .png), PDF nem e-mail (.eml, .msg).");
        Exigir(aceito.Confere(inicio), $"O conteúdo do arquivo {nome} não corresponde à extensão {extensao}.");

        return new ArquivoConferido(nome, extensao, aceito.Tipo, tamanho);
    }

    private static bool Comeca(byte[] inicio, byte[] assinatura) => inicio.AsSpan().StartsWith(assinatura);

    /// <summary>E-mail em texto (RFC 5322): sem bytes nulos e começando por um cabeçalho "Nome: valor".</summary>
    private static bool EhMensagemDeEmail(byte[] inicio)
    {
        if (inicio.Length == 0 || inicio.Contains((byte)0))
        {
            return false;
        }

        var nomeCabecalho = 0;
        foreach (var b in inicio)
        {
            if (b == ':')
            {
                return nomeCabecalho > 0;
            }

            // Nome de cabeçalho: caracteres visíveis do ASCII, exceto ':'.
            if (b is < 33 or > 126)
            {
                return false;
            }

            nomeCabecalho++;
        }

        return false;
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new RegraNegocioException(mensagem);
        }
    }
}
