namespace Contratacao.Web.Domain.Demandas;

/// <summary>Quem devolveu a demanda (RN02c).</summary>
internal enum OrigemCorrecao
{
    Gestor = 1,
    Sesi = 2,
}

/// <summary>Tipo da inconsistência apontada pela Gerenciadora (RN02). Define para onde a demanda volta (RN02a).</summary>
internal enum TipoInconsistencia
{
    Solicitante = 1,
    Contratual = 2,
}
