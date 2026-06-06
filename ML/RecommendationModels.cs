namespace PinAppdePromo.ML;

/// <summary>
/// Modelo de entrada para predicción de recomendaciones
/// </summary>
public class RecomendacionInput
{
    public float UsuarioId { get; set; }
    public float NegocioId { get; set; }
    public float CategoriaHash { get; set; }
    public float ZonaHash { get; set; }
    public float FrecuenciaCategoria { get; set; }
    public float FrecuenciaZona { get; set; }
    public float CalificacionPromedio { get; set; }
}

/// <summary>
/// Modelo de salida con la predicción de puntuación de recomendación
/// </summary>
public class RecomendacionOutput
{
    public float Score { get; set; }
}

/// <summary>
/// Modelo para entrenar: incluye la etiqueta
/// </summary>
public class RecomendacionTraining
{
    public float UsuarioId { get; set; }
    public float NegocioId { get; set; }
    public float CategoriaHash { get; set; }
    public float ZonaHash { get; set; }
    public float FrecuenciaCategoria { get; set; }
    public float FrecuenciaZona { get; set; }
    public float CalificacionPromedio { get; set; }

    /// <summary>
    /// Etiqueta: 0 (no recomendar) o 1 (recomendar)
    /// </summary>
    public bool Label { get; set; }
}
