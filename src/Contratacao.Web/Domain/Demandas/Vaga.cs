namespace Contratacao.Web.Domain.Demandas;

/// <summary>Vaga aberta na plataforma externa do SESI (UC08). Uma por demanda.</summary>
internal sealed class Vaga
{
    private Vaga() { } // EF Core

    internal Vaga(string linkExterno, DateTime dataAberturaUtc, Guid usuarioResponsavelId)
    {
        LinkExterno = linkExterno;
        DataAbertura = dataAberturaUtc;
        UsuarioResponsavelId = usuarioResponsavelId;
    }

    internal Guid Id { get; private set; }
    internal string LinkExterno { get; private set; } = string.Empty;
    internal DateTime DataAbertura { get; private set; }
    internal Guid UsuarioResponsavelId { get; private set; }
}
