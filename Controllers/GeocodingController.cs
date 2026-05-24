using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Services;
using System.Threading.Tasks;

namespace PinAppdePromo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeocodingController : ControllerBase
    {
        private readonly NominatimService _nominatimService;

        public GeocodingController(NominatimService nominatimService)
        {
            _nominatimService = nominatimService;
        }

        /// <summary>
        /// Busca coordenadas a partir de una dirección (Geocoding)
        /// </summary>
        [HttpPost("buscar-direccion")]
        public async Task<IActionResult> BuscarDireccion([FromBody] BuscarDireccionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Address))
                return BadRequest(new { error = "La dirección es requerida" });

            var resultado = await _nominatimService.BuscarDireccion(request.Address);

            if (resultado == null)
                return NotFound(new { error = "No se encontró la dirección" });

            var (latitude, longitude, displayName) = resultado.Value;
            return Ok(new
            {
                latitude,
                longitude,
                displayName
            });
        }

        /// <summary>
        /// Obtiene la dirección a partir de coordenadas (Reverse Geocoding)
        /// </summary>
        [HttpPost("obtener-direccion")]
        public async Task<IActionResult> ObtenerDireccion([FromBody] ObtenerDireccionRequest request)
        {
            if (request?.Latitude == null || request?.Longitude == null)
                return BadRequest(new { error = "Latitud y longitud son requeridas" });

            var direccion = await _nominatimService.ObtenerDireccion(request.Latitude.Value, request.Longitude.Value);

            if (string.IsNullOrEmpty(direccion))
                return NotFound(new { error = "No se encontró la dirección" });

            return Ok(new { displayName = direccion });
        }
    }

    public class BuscarDireccionRequest
    {
        public string? Address { get; set; }
    }

    public class ObtenerDireccionRequest
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
