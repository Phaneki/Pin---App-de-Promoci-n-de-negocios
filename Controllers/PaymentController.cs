using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using PinAppdePromo.Models;

namespace PinAppdePromo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentController(AppDbContext context, PinDbContext pinContext, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _pinContext = pinContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> CreatePreference()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Login");
            }

            var accessToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                return Content("Error: No se encontró el Access Token de Mercado Pago en la configuración.");
            }

            // La app corre en Render (producción). Usar la URL de Render siempre.
            // En local de desarrollo, usa el host dinámico como fallback.
            var configuredBaseUrl = _configuration["AppBaseUrl"];
            var baseUrl = !string.IsNullOrEmpty(configuredBaseUrl)
                ? configuredBaseUrl
                : "https://pin-app-de-promoci-n-de-negocios.onrender.com";

            var idempotencyKey = Guid.NewGuid().ToString();

            var requestBody = new
            {
                items = new[]
                {
                    new
                    {
                        id = "pin_premium",
                        title = "PIN Premium - Promoción de Negocios",
                        description = "Acceso premium para promocionar tu negocio en PIN",
                        quantity = 1,
                        currency_id = "PEN",
                        unit_price = 9.90
                    }
                },
                payer = new
                {
                    email = email
                },
                back_urls = new
                {
                    success = $"{baseUrl}/Payment/Success",
                    failure = $"{baseUrl}/Payment/Failure",
                    pending = $"{baseUrl}/Payment/Pending"
                },
                auto_return = "approved",
                external_reference = email,
                notification_url = $"{baseUrl}/Payment/Webhook"
            };

            var jsonOptions = new JsonSerializerOptions();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // X-Idempotency-Key es requerido por Mercado Pago en peticiones POST
            httpRequest.Headers.Add("X-Idempotency-Key", idempotencyKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

            // Usar un cliente HTTP fresco para evitar problemas de pool/headers residuales
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "PinApp/1.0");

            try
            {
                var response = await httpClient.SendAsync(httpRequest);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var initPoint = doc.RootElement.GetProperty("init_point").GetString();
                    return Redirect(initPoint!);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Content($"Error al crear la preferencia de pago de Mercado Pago. Código HTTP: {response.StatusCode}. Detalle: {errorContent}");
            }
            catch (Exception ex)
            {
                return Content($"Ocurrió un error al conectar con Mercado Pago: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (!string.IsNullOrEmpty(email))
            {
                var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                if (localUser != null)
                {
                    localUser.IsPremium = true;
                    await _context.SaveChangesAsync();
                }

                var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (pinUser != null)
                {
                    pinUser.IsPremium = true;
                    
                    // Promover automáticamente los negocios del usuario
                    var userBusinesses = await _pinContext.Businesses
                        .Where(b => b.OwnerId == pinUser.UserId && b.Status == "Approved")
                        .ToListAsync();
                        
                    foreach (var business in userBusinesses)
                    {
                        business.Status = "Promoted";
                    }

                    await _pinContext.SaveChangesAsync();
                }
                
                HttpContext.Session.SetString("IsPremium", "True");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Failure()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Pending()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook([FromQuery] string topic, [FromQuery] string id, [FromQuery(Name = "data.id")] string dataId)
        {
            // Mercado Pago envía notificaciones de tipo webhook.
            var paymentId = id ?? dataId;
            if (string.IsNullOrEmpty(paymentId) && (topic == "payment" || HttpContext.Request.Query.ContainsKey("data.id")))
            {
                paymentId = HttpContext.Request.Query["data.id"];
            }

            if (string.IsNullOrEmpty(paymentId))
            {
                return Ok();
            }

            var accessToken = _configuration["MercadoPago:AccessToken"];

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var status = doc.RootElement.GetProperty("status").GetString();
                    var externalReference = doc.RootElement.GetProperty("external_reference").GetString();

                    if (status == "approved" && !string.IsNullOrEmpty(externalReference))
                    {
                        var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == externalReference);
                        if (localUser != null)
                        {
                            localUser.IsPremium = true;
                            await _context.SaveChangesAsync();
                        }

                        var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == externalReference);
                        if (pinUser != null)
                        {
                            pinUser.IsPremium = true;
                            
                            // Promover automáticamente los negocios del usuario
                            var userBusinesses = await _pinContext.Businesses
                                .Where(b => b.OwnerId == pinUser.UserId && b.Status == "Approved")
                                .ToListAsync();
                                
                            foreach (var business in userBusinesses)
                            {
                                business.Status = "Promoted";
                            }

                            await _pinContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch
            {
                // En webhooks es importante retornar 200/Ok para evitar que Mercado Pago reintente infinitamente en caso de excepciones internas
            }

            return Ok();
        }
    }
}
