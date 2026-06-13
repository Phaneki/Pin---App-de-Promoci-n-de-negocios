using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace PinAppdePromo.Services
{
    public class GooglePlaceInfo
    {
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
        public string OpeningHours { get; set; }
    }

    /// <summary>
    /// 🌐 Servicio para obtener información y fotos de negocios desde Google Places API
    /// Soporta búsqueda por nombre y ubicación, y extrae URLs de fotos de alta calidad
    /// </summary>
    public interface IGooglePlacesService
    {
        Task<string> GetBusinessPhotoUrlAsync(string businessName, string address);
        Task<string> GetPlaceIdAsync(string businessName, string address);
        Task<string> GetPlacePhotoAsync(string placeId);
        Task<GooglePlaceInfo> GetBusinessInfoAsync(string businessName, string address);
    }

    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesService> _logger;
        private readonly string _apiKey;
        private const string TextSearchUrl = "https://maps.googleapis.com/maps/api/place/textsearch/json";
        private const string PlaceDetailsUrl = "https://maps.googleapis.com/maps/api/place/details/json";
        private const string PhotoMaxWidth = "800";

        public GooglePlacesService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GooglePlacesService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["GoogleMaps:ApiKey"] ?? "";
        }

        /// <summary>
        /// 🔍 Obtiene la URL de una foto de un negocio buscando por nombre y dirección
        /// </summary>
        public async Task<string> GetBusinessPhotoUrlAsync(string businessName, string address)
        {
            try
            {
                // Validar que tenemos API key
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("TU_API_KEY"))
                {
                    _logger.LogWarning("⚠️ Google Places API Key no está configurada. Usando fallback.");
                    return GetFallbackPhotoUrl(businessName);
                }

                // 1️⃣ Obtener el Place ID del negocio
                var placeId = await GetPlaceIdAsync(businessName, address);
                if (string.IsNullOrEmpty(placeId))
                {
                    _logger.LogWarning($"No se encontró Place ID para: {businessName}");
                    return GetFallbackPhotoUrl(businessName);
                }

                // 2️⃣ Obtener la foto del negocio usando el Place ID
                var photoUrl = await GetPlacePhotoAsync(placeId);
                
                if (string.IsNullOrEmpty(photoUrl))
                {
                    _logger.LogWarning($"No se encontró foto para: {businessName} (ID: {placeId})");
                    return GetFallbackPhotoUrl(businessName);
                }

                _logger.LogInformation($"✅ Foto obtenida para {businessName}: {photoUrl}");
                return photoUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error obteniendo foto de {businessName}: {ex.Message}");
                return GetFallbackPhotoUrl(businessName);
            }
        }

        /// <summary>
        /// 🔎 Busca un negocio por nombre y dirección, retorna su Place ID
        /// </summary>
        public async Task<string> GetPlaceIdAsync(string businessName, string address)
        {
            try
            {
                var searchQuery = string.IsNullOrEmpty(address) 
                    ? businessName 
                    : $"{businessName} {address}";

                var url = $"{TextSearchUrl}?query={Uri.EscapeDataString(searchQuery)}&key={_apiKey}";
                
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(content);
                var results = json["results"] as JArray;

                if (results == null || results.Count == 0)
                {
                    _logger.LogWarning($"Sin resultados para: {searchQuery}");
                    return null;
                }

                // Obtener el primer resultado (más relevante)
                var placeId = results[0]["place_id"]?.ToString();
                _logger.LogInformation($"Place ID encontrado para {businessName}: {placeId}");
                
                return placeId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en GetPlaceIdAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 📸 Obtiene la URL de una foto desde Google Places usando el Place ID
        /// </summary>
        public async Task<string> GetPlacePhotoAsync(string placeId)
        {
            try
            {
                // Primero, obtener los detalles del lugar para acceder a las fotos
                var detailsUrl = $"{PlaceDetailsUrl}?place_id={placeId}&fields=photos&key={_apiKey}";
                
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(detailsUrl);
                var content = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(content);
                var photos = json["result"]?["photos"] as JArray;

                if (photos == null || photos.Count == 0)
                {
                    _logger.LogWarning($"Sin fotos para Place ID: {placeId}");
                    return null;
                }

                // Obtener la referencia de la primera foto
                var photoReference = photos[0]["photo_reference"]?.ToString();
                if (string.IsNullOrEmpty(photoReference))
                {
                    return null;
                }

                // Construir URL de la foto
                var photoUrl = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth={PhotoMaxWidth}&photo_reference={photoReference}&key={_apiKey}";
                
                return photoUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en GetPlacePhotoAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 📞 Obtiene información adicional de un negocio (Teléfono, Web y Horarios)
        /// </summary>
        public async Task<GooglePlaceInfo> GetBusinessInfoAsync(string businessName, string address)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("TU_API_KEY")) return null;

                var placeId = await GetPlaceIdAsync(businessName, address);
                if (string.IsNullOrEmpty(placeId)) return null;

                var detailsUrl = $"{PlaceDetailsUrl}?place_id={placeId}&fields=formatted_phone_number,website,opening_hours&key={_apiKey}";
                
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(detailsUrl);
                var content = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(content);
                var result = json["result"];

                if (result == null) return null;

                var info = new GooglePlaceInfo();
                info.PhoneNumber = result["formatted_phone_number"]?.ToString();
                info.Website = result["website"]?.ToString();

                var weekdayTextArray = result["opening_hours"]?["weekday_text"] as JArray;
                if (weekdayTextArray != null && weekdayTextArray.Count > 0)
                {
                    info.OpeningHours = string.Join(", ", weekdayTextArray.Select(x => x.ToString()));
                }

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en GetBusinessInfoAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎨 Retorna una URL de imagen placeholder si no hay foto disponible
        /// Usa colores y emojis basados en el nombre del negocio
        /// </summary>
        private string GetFallbackPhotoUrl(string businessName)
        {
            // Usar UI Avatars como fallback - genera avatares coloridos basados en el nombre
            var encodedName = Uri.EscapeDataString(businessName);
            return $"https://ui-avatars.com/api/?name={encodedName}&size=400&background=random&color=fff&font-size=0.33";
        }
    }
}
