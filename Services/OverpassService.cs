using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PinAppdePromo.Models;

namespace PinAppdePromo.Services
{
    // Modelo para deserializar la respuesta de Overpass API
    public class OverpassResponse
    {
        [JsonPropertyName("elements")]
        public List<OverpassElement> Elements { get; set; } = new();
    }

    public class OverpassElement
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("center")]
        public OverpassCenter? Center { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    public class OverpassCenter
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    public class OverpassService
    {
        private readonly HttpClient _httpClient;
        private readonly PinDbContext _dbContext;
        private readonly ILogger<OverpassService> _logger;
        private const string OVERPASS_API_URL = "https://overpass-api.de/api/interpreter";

        public OverpassService(HttpClient httpClient, PinDbContext dbContext, ILogger<OverpassService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Importa negocios desde Overpass API (OpenStreetMap) a la base de datos local.
        /// </summary>
        /// <param name="latitude">Latitud del centro de búsqueda</param>
        /// <param name="longitude">Longitud del centro de búsqueda</param>
        /// <param name="radiusInMeters">Radio de búsqueda en metros</param>
        /// <returns>Cantidad de negocios importados</returns>
        public async Task<int> ImportarNegociosCercanos(double latitude, double longitude, int radiusInMeters = 1000)
        {
            _logger.LogInformation("🎯 Parámetros recibidos: lat={lat}, lon={lon}, radio={radio}", latitude, longitude, radiusInMeters);
            
            // 1. Construir consulta simple para testear
            var query = $"[out:json];(node[amenity=restaurant](around:{radiusInMeters},{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}););out center;";

            _logger.LogInformation("📝 Consulta: {query}", query);

            try
            {
                // 2. Enviar solicitud a Overpass API
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
                
                using var request = new HttpRequestMessage(HttpMethod.Post, OVERPASS_API_URL)
                {
                    Content = content
                };
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("User-Agent", "Pin-App");
                
                _logger.LogInformation("🌐 POST a {url}", OVERPASS_API_URL);
                var response = await _httpClient.SendAsync(request, cts.Token);
                
                var responseText = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Status: {status}, Body: {body}", response.StatusCode, responseText.Substring(0, Math.Min(500, responseText.Length)));
                    return 0;
                }

                _logger.LogInformation("✅ Respondió con {bytes} bytes", responseText.Length);

                var overpassData = JsonSerializer.Deserialize<OverpassResponse>(responseText);

                if (overpassData?.Elements == null || overpassData.Elements.Count == 0)
                {
                    _logger.LogWarning("⚠️ Sin elementos");
                    return 0;
                }
                _logger.LogInformation("📦 {count} elementos", overpassData.Elements.Count);

                // 3. Obtener categorías existentes para mapear
                var categorias = await _dbContext.Categories.ToListAsync();
                var categoriaDefault = categorias.FirstOrDefault() ?? new Category { Name = "General" };

                int importados = 0;

                foreach (var element in overpassData.Elements)
                {
                    if (element.Tags == null) continue;

                    // Obtener nombre del negocio
                    var name = GetTagValue(element.Tags, "name", "operator", "brand");
                    if (string.IsNullOrEmpty(name)) continue;

                    // Verificar si ya existe en la BD por ExternalId o por nombre+ubicación
                    var existe = await _dbContext.Businesses.AnyAsync(b =>
                        b.ExternalId == element.Id.ToString() && b.Source == "OSM");

                    if (existe) continue;

                    // Obtener coordenadas
                    double lat = element.Lat ?? element.Center?.Lat ?? latitude;
                    double lon = element.Lon ?? element.Center?.Lon ?? longitude;

                    // Determinar categoría
                    var categoryId = await MapearCategoria(element.Tags, categorias, categoriaDefault);

                    // Construir dirección
                    var address = BuildAddress(element.Tags);

                    // Obtener teléfono
                    var phone = GetTagValue(element.Tags, "phone", "contact:phone");

                    // Obtener horarios
                    var openingHours = GetTagValue(element.Tags, "opening_hours");

                    // Crear el negocio
                    var business = new Business
                    {
                        OwnerId = 1, // Owner por defecto (admin)
                        TradeName = Truncate(name, 200),
                        Description = BuildDescription(element.Tags, name),
                        Address = Truncate(address, 500),
                        Latitude = (decimal)lat,
                        Longitude = (decimal)lon,
                        CategoryId = categoryId,
                        Status = "Approved", // Viene pre-aprobado de OSM
                        ContactPhone = Truncate(phone, 50),
                        RUC = "", // RUC no disponible desde OSM
                        CreatedAt = DateTime.UtcNow,
                        Source = "OSM",
                        ExternalId = element.Id.ToString(),
                        LastSyncedAt = DateTime.UtcNow
                    };

                    _dbContext.Businesses.Add(business);
                    await _dbContext.SaveChangesAsync();

                    // Agregar imagen por defecto si no tiene
                    var imageUrl = GetBusinessImageUrl(element.Tags, name);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _dbContext.BusinessImages.Add(new BusinessImage
                        {
                            BusinessId = business.BusinessId,
                            ImageUrl = imageUrl
                        });
                        await _dbContext.SaveChangesAsync();
                    }

                    // NOTA: Los horarios (opening_hours) se guardan en la descripción por ahora
                    if (!string.IsNullOrEmpty(openingHours))
                    {
                        business.Description += $" | Horarios: {openingHours}";
                    }

                    importados++;
                }

                return importados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al importar negocios de Overpass API");
                throw new Exception($"Error al importar negocios de Overpass API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene el valor de una etiqueta de OpenStreetMap probando múltiples keys
        /// </summary>
        private string GetTagValue(Dictionary<string, string> tags, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (tags.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                    return value;
            }
            return "";
        }

        /// <summary>
        /// Construye una dirección a partir de los tags de OSM
        /// </summary>
        private string BuildAddress(Dictionary<string, string> tags)
        {
            var parts = new List<string>();

            var street = GetTagValue(tags, "addr:street", "street");
            var housenumber = GetTagValue(tags, "addr:housenumber", "housenumber");
            var city = GetTagValue(tags, "addr:city", "city");
            var district = GetTagValue(tags, "addr:district", "district");

            if (!string.IsNullOrEmpty(street))
            {
                var dir = street;
                if (!string.IsNullOrEmpty(housenumber))
                    dir += " " + housenumber;
                parts.Add(dir);
            }

            if (!string.IsNullOrEmpty(district))
                parts.Add(district);

            if (!string.IsNullOrEmpty(city))
                parts.Add(city);

            return parts.Count > 0 ? string.Join(", ", parts) : "Dirección no disponible";
        }

        /// <summary>
        /// Construye una descripción basada en los tags del negocio
        /// </summary>
        private string BuildDescription(Dictionary<string, string> tags, string name)
        {
            var descParts = new List<string>();

            var amenity = GetTagValue(tags, "amenity", "shop", "leisure", "tourism");
            var cuisine = GetTagValue(tags, "cuisine");
            var website = GetTagValue(tags, "website", "contact:website", "url");

            if (!string.IsNullOrEmpty(amenity))
            {
                descParts.Add($"Categoría: {amenity}");
            }

            if (!string.IsNullOrEmpty(cuisine))
            {
                descParts.Add($"Tipo de cocina: {cuisine}");
            }

            if (!string.IsNullOrEmpty(website))
            {
                descParts.Add($"Sitio web: {website}");
            }

            descParts.Add("Importado de OpenStreetMap");

            return string.Join(" | ", descParts);
        }

        /// <summary>
        /// Obtiene una URL de imagen para el negocio basada en su tipo
        /// </summary>
        private string GetBusinessImageUrl(Dictionary<string, string> tags, string name)
        {
            // 1. Intentar obtener una foto REAL si OpenStreetMap la tiene registrada
            var realImage = GetTagValue(tags, "image", "image:url", "contact:image", "contact:instagram", "contact:facebook");
            if (!string.IsNullOrEmpty(realImage) && realImage.StartsWith("http"))
            {
                return realImage;
            }

            // 2. Si no tiene foto real, generamos un placeholder atractivo con su nombre
            var shortName = string.IsNullOrEmpty(name) ? "Negocio" : (name.Length > 20 ? name.Substring(0, 20) : name);
            // Reemplazamos los espacios por + para la URL de placehold.co
            var textUrl = Uri.EscapeDataString(shortName);
            return $"https://placehold.co/600x400/ff6b00/ffffff?text={textUrl}";
        }

        /// <summary>
        /// Determina la categoría en la BD local basada en los tags de OSM
        /// </summary>
        private async Task<int> MapearCategoria(Dictionary<string, string> tags, List<Category> categorias, Category defaultCat)
        {
            var amenity = GetTagValue(tags, "amenity", "shop", "leisure", "tourism");

            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "restaurant", "Restaurantes" },
                { "fast_food", "Restaurantes" },
                { "cafe", "Restaurantes" },
                { "bar", "Restaurantes" },
                { "pub", "Restaurantes" },
                { "electronics", "Tecnología" },
                { "computer", "Tecnología" },
                { "mobile_phone", "Tecnología" },
                { "car_repair", "Servicios Automotrices" },
                { "car_wash", "Servicios Automotrices" },
                { "fuel", "Servicios Automotrices" },
                { "pharmacy", "Salud y Belleza" },
                { "cosmetics", "Salud y Belleza" },
                { "hairdresser", "Salud y Belleza" },
                { "beauty", "Salud y Belleza" },
                { "clinic", "Salud y Belleza" },
                { "hospital", "Salud y Belleza" },
                { "bank", "Servicios" },
                { "school", "Educación" },
                { "university", "Educación" },
                { "fitness_centre", "Deportes" },
                { "sports_centre", "Deportes" },
                { "hotel", "Hospedaje" },
                { "hostel", "Hospedaje" },
                { "supermarket", "Tiendas" },
                { "convenience", "Tiendas" },
                { "clothes", "Tiendas" },
                { "shoes", "Tiendas" },
                { "bakery", "Tiendas" }
            };

            if (!string.IsNullOrEmpty(amenity) && mapping.TryGetValue(amenity, out var catName))
            {
                var cat = categorias.FirstOrDefault(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase));
                if (cat != null) return cat.CategoryId;
            }

            // Si no hay mapping, crear o devolver default
            var general = categorias.FirstOrDefault(c => c.Name == "General")
                ?? new Category { Name = "General" };
            return (general.CategoryId != 0) ? general.CategoryId : defaultCat.CategoryId;
        }

        /// <summary>
        /// Trunca un string a una longitud máxima
        /// </summary>
        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}