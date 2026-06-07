namespace PinAppdePromo.ML;

/// <summary>
/// Modelo de entrada para predicción de recomendaciones
/// </summary>
public class RecomendacionInput
{
    public string UsuarioId { get; set; } = string.Empty;
    public string NegocioId { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
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
    public float Probability { get; set; }
    public bool PredictedLabel { get; set; }
}

/// <summary>
/// Modelo para entrenar: incluye la etiqueta
/// </summary>
public class RecomendacionTraining
{
    public string UsuarioId { get; set; } = string.Empty;
    public string NegocioId { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public float FrecuenciaCategoria { get; set; }
    public float FrecuenciaZona { get; set; }
    public float CalificacionPromedio { get; set; }

    /// <summary>
    /// Etiqueta: 0 (no recomendar) o 1 (recomendar)
    /// </summary>
    public bool Label { get; set; }
}
