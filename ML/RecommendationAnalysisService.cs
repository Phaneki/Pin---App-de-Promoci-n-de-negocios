using PinAppdePromo.Models;

namespace PinAppdePromo.ML;

/// <summary>
/// Servicio que analiza patrones de búsqueda y genera recomendaciones
/// </summary>
public class RecommendationAnalysisService
{
    private readonly RecommendationService _mlService;

    public RecommendationAnalysisService(RecommendationService mlService)
    {
        _mlService = mlService;
    }

    /// <summary>
    /// Obtiene recomendaciones personalizadas para un usuario
    /// </summary>
    public List<NegocioRecomendadoDTO> GetPersonalizedRecommendations(
        int usuarioId,
        List<BusquedaUsuario> historialBusquedas,
        List<NegocioDTO> negociosDisponibles,
        int cantidad = 10)
    {
        if (!historialBusquedas.Any())
        {
            return negociosDisponibles
                .OrderByDescending(n => n.Calificacion)
                .Take(cantidad)
                .Select(n => new NegocioRecomendadoDTO
                {
                    NegocioDTO = n,
                    PuntajeRecomendacion = 0.5f,
                    Razon = "Recomendación por calificación general"
                })
                .ToList();
        }

        // Calcular frecuencias de búsqueda
        var frecuenciasCategorias = CalcularFrecuencias(historialBusquedas.Select(b => b.Categoria));
        var frecuenciasZonas = CalcularFrecuencias(historialBusquedas.Select(b => b.Zona));

        // Obtener zona más común
        var zonaMasComun = frecuenciasZonas.OrderByDescending(f => f.Value).FirstOrDefault().Key ?? string.Empty;

        var busquedasConCalificacion = historialBusquedas.Where(b => b.Calificacion.HasValue).ToList();
        var calificacionPromedio = busquedasConCalificacion.Any() 
            ? busquedasConCalificacion.Average(b => b.Calificacion.Value) 
            : 0f;

        // Generar recomendaciones
        var recomendaciones = negociosDisponibles
            .Select(negocio =>
            {
                var score = _mlService.PredictRecommendationScore(
                    usuarioId,
                    negocio.Id,
                    negocio.Categoria,
                    ExtractZone(negocio.Direccion),
                    ConvertirADictionary(frecuenciasCategorias),
                    ConvertirADictionary(frecuenciasZonas),
                    calificacionPromedio);

                var razon = GenerarRazon(negocio, zonaMasComun, frecuenciasCategorias);

                return new NegocioRecomendadoDTO
                {
                    NegocioDTO = negocio,
                    PuntajeRecomendacion = score,
                    Razon = razon
                };
            })
            .OrderByDescending(r => r.PuntajeRecomendacion)
            .Take(cantidad)
            .ToList();

        return recomendaciones;
    }

    /// <summary>
    /// Entrena el modelo con datos históricos
    /// </summary>
    public void ReentrenarModelo(List<BusquedaUsuario> busquedas)
    {
        _mlService.TrainModel(busquedas);
    }

    /// <summary>
    /// Calcula la frecuencia de cada valor en una colección
    /// </summary>
    private Dictionary<string, int> CalcularFrecuencias(IEnumerable<string> valores)
    {
        return valores
            .Where(v => !string.IsNullOrEmpty(v))
            .GroupBy(v => v)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(f => f.Value)
            .Take(10)
            .ToDictionary(f => f.Key, f => f.Value);
    }

    /// <summary>
    /// Extrae la zona de una dirección (simplificado)
    /// </summary>
    private string ExtractZone(string direccion)
    {
        if (string.IsNullOrEmpty(direccion))
            return "Desconocida";

        var palabras = direccion.Split(',');
        return palabras.Length > 0 ? palabras[^1].Trim() : "Desconocida";
    }

    /// <summary>
    /// Convierte IEnumerable a Dictionary
    /// </summary>
    private Dictionary<string, int> ConvertirADictionary(Dictionary<string, int> dict)
    {
        return dict;
    }

    /// <summary>
    /// Genera una razón explicativa para la recomendación
    /// </summary>
    private string GenerarRazon(NegocioDTO negocio, string zonaMasComun, Dictionary<string, int> frecuencias)
    {
        var razones = new List<string>();

        if (frecuencias.ContainsKey(negocio.Categoria) && frecuencias[negocio.Categoria] > 0)
        {
            razones.Add($"Te gusta la categoría {negocio.Categoria}");
        }

        if (negocio.Calificacion >= 4.0)
        {
            razones.Add($"Tiene excelente calificación ({negocio.Calificacion:F1}/5)");
        }

        if (razones.Count == 0)
        {
            razones.Add("Recomendación basada en tu perfil");
        }

        return string.Join(" • ", razones);
    }
}
