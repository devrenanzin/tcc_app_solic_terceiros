namespace Contratacao.Web.Domain.Usuarios;

/// <summary>Vínculo do Gestor com um contrato; um Gestor pode ter vários (revisão de 05/10/2026).</summary>
internal sealed class GestorContrato
{
    private GestorContrato() { } // EF Core

    internal GestorContrato(Guid gestorId, Guid contratoId)
    {
        GestorId = gestorId;
        ContratoId = contratoId;
    }

    internal Guid GestorId { get; private set; }
    internal Guid ContratoId { get; private set; }
}
