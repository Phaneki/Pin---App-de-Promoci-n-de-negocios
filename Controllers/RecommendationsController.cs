using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Models;
using PinAppdePromo.ML;
using Microsoft.EntityFrameworkCore;

namespace PinAppdePromo.Controllers;

/// <summary>
/// Controlador API para obtener recomendaciones personalizadas
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly PinDbContext _context;
    private readonly RecommendationAnalysisService _analysisService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        PinDbContext context,
        RecommendationAnalysisService analysisService,
        ILogger<RecommendationsController> logger)
    {
        _context = context;
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene recomendaciones personalizadas para el usuario actual
    /// </summary>
    /// <param name="usuarioId">ID del usuario</param>
    /// <param name="cantidad">Número de recomendaciones (máximo 20)</param>
    [HttpGet("personalizadas/{usuarioId}")]
    public ActionResult<List<RecommendationResponse>> GetPersonalizedRecommendations(
        int usuarioId,
        [FromQuery] int cantidad = 10)
    {
        try
        {
            // Validar parámetros
            cantidad = Math.Min(Math.Max(cantidad, 1), 20);

            // Obtener historial de búsquedas del usuario
            var historialBusquedas = _context.BusquedasUsuario
                .Where(b => b.UsuarioId == usuarioId)
                .ToList();


            // Obtener todos los negocios disponibles (adaptado a Business)
            var negociosDisponibles = _context.Businesses
                .Where(n => n.Status == "Active" || n.Status == "Verified")
                .Include(b => b.Category)
                .ToList();

            // Mapear a lista de PinAppdePromo.ML.NegocioDTO
            var negociosDisponiblesML = negociosDisponibles.Select(b => new PinAppdePromo.ML.NegocioDTO
            {
                Id = b.BusinessId,
                Nombre = b.TradeName ?? string.Empty,
                Categoria = b.Category != null ? b.Category.Name : "General",
                Direccion = b.Address ?? string.Empty,
                Calificacion = (double)(b.Reviews != null && b.Reviews.Any()
                    ? b.Reviews.Average(r => r.Rating)
                    : 0),
                ImagenUrl = b.Images != null && b.Images.Any()
                    ? b.Images.First().ImageUrl
                    : "~/images/default.jpg"
            }).ToList();

            if (!negociosDisponibles.Any())
            {
                return Ok(new List<RecommendationResponse>());
            }

            // Obtener recomendaciones
            var recomendaciones = _analysisService.GetPersonalizedRecommendations(
                usuarioId,
                historialBusquedas,
                negociosDisponiblesML,
                cantidad);

            // Convertir a response
            var response = recomendaciones.Select(r => new RecommendationResponse
            {
                NegocioId = r.NegocioDTO.Id,
                Nombre = r.NegocioDTO.Nombre,
                Categoria = r.NegocioDTO.Categoria,
                Calificacion = r.NegocioDTO.Calificacion,
                PuntajeRecomendacion = r.PuntajeRecomendacion,
                Razon = r.Razon,
                ImagenUrl = r.NegocioDTO.ImagenUrl
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo recomendaciones para usuario {UsuarioId}", usuarioId);
            return StatusCode(500, new { error = "Error al procesar recomendaciones" });
        }
    }

    /// <summary>
    /// Registra una búsqueda del usuario para mejorar recomendaciones
    /// </summary>
    [HttpPost("registrar-busqueda")]
    public ActionResult RegisterSearch([FromBody] BusquedaRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var busqueda = new BusquedaUsuario
            {
                UsuarioId = request.UsuarioId,
                NegocioId = request.NegocioId,
                Categoria = request.Categoria,
                Zona = request.Zona,
                FechaBusqueda = DateTime.UtcNow,
                TipoInteraccion = request.TipoInteraccion,
                Calificacion = request.Calificacion
            };

            _context.BusquedasUsuario.Add(busqueda);
            _context.SaveChanges();

            // Entrenar modelo cada 50 nuevas búsquedas
            var totalBusquedas = _context.BusquedasUsuario.Count();
            if (totalBusquedas % 50 == 0)
            {
                var todasLasBusquedas = _context.BusquedasUsuario.ToList();
                _analysisService.ReentrenarModelo(todasLasBusquedas);
            }

            return Ok(new { mensaje = "Búsqueda registrada correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando búsqueda");
            return StatusCode(500, new { error = "Error al registrar búsqueda" });
        }
    }

    /// <summary>
    /// Obtiene recomendaciones por categoría específica
    /// </summary>
    [HttpGet("por-categoria/{usuarioId}")]
    public ActionResult<List<RecommendationResponse>> GetRecommendationsByCategory(
        int usuarioId,
        [FromQuery] string categoria,
        [FromQuery] int cantidad = 10)
    {
        try
        {
            var historialBusquedas = _context.BusquedasUsuario
                .Where(b => b.UsuarioId == usuarioId)
                .ToList();

            var negociosPorCategoria = _context.Businesses
                .Where(n => (n.Status == "Active" || n.Status == "Verified") && n.Category != null && n.Category.Name == categoria)
                .Include(b => b.Category)
                .Select(b => new NegocioDTO
                {
                    Id = b.BusinessId,
                    Nombre = b.TradeName,
                    Categoria = b.Category != null ? b.Category.Name : "General",
                    Direccion = b.Address,
                    Calificacion = (double)(b.Reviews != null && b.Reviews.Any() 
                        ? b.Reviews.Average(r => r.Rating) 
                        : 0),
                    ImagenUrl = b.Images != null && b.Images.Any() 
                        ? b.Images.First().ImageUrl 
                        : "~/images/default.jpg"
                })
                .ToList();

            if (!negociosPorCategoria.Any())
                return Ok(new List<RecommendationResponse>());

            // Mapear a lista de ML.NegocioDTO para el servicio de recomendaciones
            var negociosPorCategoriaML = negociosPorCategoria.Select(b => new PinAppdePromo.ML.NegocioDTO
            {
                Id = b.Id,
                Nombre = b.Nombre ?? string.Empty,
                Categoria = b.Categoria ?? string.Empty,
                Direccion = b.Direccion ?? string.Empty,
                Calificacion = b.Calificacion,
                ImagenUrl = b.ImagenUrl
            }).ToList();

            var recomendaciones = _analysisService.GetPersonalizedRecommendations(
                usuarioId,
                historialBusquedas,
                negociosPorCategoriaML,
                cantidad);

            var response = recomendaciones.Select(r => new RecommendationResponse
            {
                NegocioId = r.NegocioDTO.Id,
                Nombre = r.NegocioDTO.Nombre,
                Categoria = r.NegocioDTO.Categoria,
                Calificacion = r.NegocioDTO.Calificacion,
                PuntajeRecomendacion = r.PuntajeRecomendacion,
                Razon = r.Razon,
                ImagenUrl = r.NegocioDTO.ImagenUrl
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo recomendaciones por categoría");
            return StatusCode(500, new { error = "Error procesando solicitud" });
        }
    }

    /// <summary>
    /// Obtiene el historial de búsquedas de un usuario
    /// </summary>
    [HttpGet("historial/{usuarioId}")]
    public ActionResult<List<SearchHistoryResponse>> GetSearchHistory(
        int usuarioId,
        [FromQuery] int dias = 30)
    {
        try
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-dias);

            var historial = _context.BusquedasUsuario
                .Where(b => b.UsuarioId == usuarioId && b.FechaBusqueda >= fechaLimite)
                .OrderByDescending(b => b.FechaBusqueda)
                .Select(b => new SearchHistoryResponse
                {
                    NegocioId = b.NegocioId,
                    Categoria = b.Categoria,
                    Zona = b.Zona,
                    FechaBusqueda = b.FechaBusqueda,
                    TipoInteraccion = b.TipoInteraccion,
                    Calificacion = b.Calificacion
                })
                .ToList();

            return Ok(historial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo historial");
            return StatusCode(500, new { error = "Error al obtener historial" });
        }
    }
}

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
/// DTO para respuesta de recomendaciones
/// </summary>
public class RecommendationResponse
{
    public int NegocioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public double Calificacion { get; set; }
    public float PuntajeRecomendacion { get; set; }
    public string Razon { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
}

/// <summary>
/// DTO para solicitud de búsqueda
/// </summary>
public class BusquedaRequest
{
    public int UsuarioId { get; set; }
    public int NegocioId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    /// <summary>
    /// 0=búsqueda, 1=click, 2=favorito, 3=reserva
    /// </summary>
    public int TipoInteraccion { get; set; } = 0;
    public float? Calificacion { get; set; }
}

/// <summary>
/// DTO para historial de búsquedas
/// </summary>
public class SearchHistoryResponse
{
    public int NegocioId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public DateTime FechaBusqueda { get; set; }
    public int TipoInteraccion { get; set; }
    public float? Calificacion { get; set; }
}
