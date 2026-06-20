namespace PinAppdePromo.ML;

/// <summary>
/// DTO para datos del negocio
/// </summary>
public class NegocioDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public double Calificacion { get; set; }
    public string? ImagenUrl { get; set; }
}

/// <summary>
/// Modelo que representa un negocio con su puntuación de recomendación
/// </summary>
public class NegocioRecomendadoDTO
{
    public NegocioDTO NegocioDTO { get; set; } = new();
    public float PuntajeRecomendacion { get; set; }
    public string Razon { get; set; } = string.Empty;
}
