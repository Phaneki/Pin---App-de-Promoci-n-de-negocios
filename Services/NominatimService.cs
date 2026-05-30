using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PinAppdePromo.Services
{
    public class NominatimResponse
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = "";

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = "";

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("address")]
        public Dictionary<string, string> Address { get; set; } = new();
    }

    public class NominatimSearchResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = "";

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = "";

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = "";
    }

    public class NominatimService
    {
        private readonly HttpClient _httpClient;
        private const string NOMINATIM_BASE_URL = "https://nominatim.openstreetmap.org";
        private const string USER_AGENT = "Pin-App-Promocion-Negocios/1.0 (https://github.com/Daliaga-c/Pin)";

        public NominatimService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Configurar User-Agent por defecto (requerido por Nominatim)
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            }
        }

        /// <summary>
        /// Busca una dirección y retorna sus coordenadas
        /// </summary>
        public async Task<(double latitude, double longitude, string displayName)?> BuscarDireccion(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"{NOMINATIM_BASE_URL}/search?format=json&q={encodedAddress}&limit=1&countrycodes=PE";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimSearchResult>>(jsonResponse);

                if (results == null || results.Count == 0)
                    return null;

                var result = results[0];
                if (double.TryParse(result.Lat, out var lat) && double.TryParse(result.Lon, out var lon))
                {
                    return (lat, lon, result.DisplayName);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al buscar dirección en Nominatim: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la dirección a partir de coordenadas (reverse geocoding)
        /// </summary>
        public async Task<string?> ObtenerDireccion(double latitude, double longitude)
        {
            try
            {
                var url = $"{NOMINATIM_BASE_URL}/reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<NominatimResponse>(jsonResponse);

                return result?.DisplayName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener dirección en Nominatim: {ex.Message}");
                return null;
            }
        }
    }
}
