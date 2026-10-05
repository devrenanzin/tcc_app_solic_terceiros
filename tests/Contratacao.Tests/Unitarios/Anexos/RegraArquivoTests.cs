using System.Text;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Comum;

namespace Contratacao.Tests.Unitarios.Anexos;

/// <summary>Regras de arquivo da seção 18: tipos aceitos, conteúdo conferido e limite de 10 MB.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed class RegraArquivoTests
{
    internal static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%conteudo de teste");
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    internal static readonly byte[] Msg = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00];
    internal static readonly byte[] Eml = Encoding.ASCII.GetBytes("From: pessoa@ucl.br\r\nSubject: De acordo\r\n\r\nOk.");

    [Theory]
    [InlineData("de-acordo.pdf", "application/pdf")]
    [InlineData("foto.PNG", "image/png")]
    [InlineData("foto.jpg", "image/jpeg")]
    [InlineData("foto.jpeg", "image/jpeg")]
    [InlineData("mensagem.msg", "application/vnd.ms-outlook")]
    [InlineData("mensagem.eml", "message/rfc822")]
    public void Aceita_imagem_pdf_e_email_reconhecidos_pelo_conteudo(string nome, string tipo)
    {
        var conteudo = Path.GetExtension(nome).ToLowerInvariant() switch
        {
            ".pdf" => Pdf,
            ".png" => Png,
            ".msg" => Msg,
            ".eml" => Eml,
            _ => Jpeg,
        };

        var arquivo = RegraArquivo.Conferir(nome, conteudo.Length, conteudo);

        Assert.Equal(tipo, arquivo.TipoArquivo);
        Assert.Equal(nome, arquivo.NomeArquivo);
    }

    [Fact]
    public void Recusa_extensao_fora_da_lista()
        => Assert.Throws<RegraNegocioException>(() => RegraArquivo.Conferir("planilha.xlsx", 10, [0x50, 0x4B, 0x03, 0x04]));

    [Theory]
    [InlineData("falso.pdf")]
    [InlineData("falso.png")]
    [InlineData("falso.msg")]
    [InlineData("falso.eml")]
    public void Recusa_conteudo_que_nao_corresponde_a_extensao(string nome)
    {
        byte[] executavel = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00];

        var erro = Assert.Throws<RegraNegocioException>(() => RegraArquivo.Conferir(nome, executavel.Length, executavel));
        Assert.Contains("conteúdo", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Recusa_arquivo_vazio_e_acima_de_10_mb()
    {
        Assert.Throws<RegraNegocioException>(() => RegraArquivo.Conferir("vazio.pdf", 0, []));
        Assert.Throws<RegraNegocioException>(() => RegraArquivo.Conferir("grande.pdf", RegraArquivo.TamanhoMaximo + 1, Pdf));
        RegraArquivo.Conferir("limite.pdf", RegraArquivo.TamanhoMaximo, Pdf);
    }

    [Fact]
    public void Guarda_so_o_nome_sem_o_caminho_enviado_pelo_navegador()
        => Assert.Equal("de-acordo.pdf", RegraArquivo.Conferir(@"C:\Users\pessoa\de-acordo.pdf", Pdf.Length, Pdf).NomeArquivo);
}
