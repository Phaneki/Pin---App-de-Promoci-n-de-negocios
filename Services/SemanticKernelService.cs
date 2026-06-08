using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PinAppdePromo.Plugins;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace PinAppdePromo.Services
{
    // --- Solución para el bug 404 de la librería Alpha de Microsoft ---
    public class GeminiHttpHandler : DelegatingHandler
    {
        public GeminiHttpHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? "";
            
            // Forzar el uso de la API v1beta que sí soporta los modelos 1.5
            if (url.Contains("/v1/"))
            {
                request.RequestUri = new Uri(url.Replace("/v1/", "/v1beta/"));
            }

            var response = await base.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de Google. URL: {request.RequestUri}. Body: {body}");
            }

            return response;
        }
    }

    public interface ISemanticKernelService
    {
        Task<string> GetRecommendationAsync(string userMessage);
    }

    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;

        public SemanticKernelService(IConfiguration configuration, PinAppdePromo.Models.PinDbContext dbContext)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "PEGA_AQUI_TU_NUEVA_CLAVE_DE_GEMINI") 
            {
                 throw new System.Exception("API KEY NO ENCONTRADA. Asegúrate de estar en modo Development o copia la clave directamente.");
            }

            var builder = Kernel.CreateBuilder();
            
            // Inyectamos el cliente HTTP modificado para corregir el bug de Microsoft
            var handler = new GeminiHttpHandler(new HttpClientHandler());
            var customHttpClient = new HttpClient(handler);

            // Google Gemini Chat Completion
            builder.AddGoogleAIGeminiChatCompletion(
                modelId: "gemini-flash-latest",
                apiKey: apiKey,
                httpClient: customHttpClient
            );

            // Add the Business Search Plugin
            builder.Plugins.AddFromObject(new BusinessSearchPlugin(dbContext), "BusinessSearchPlugin");

            _kernel = builder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GetRecommendationAsync(string userMessage)
        {
            try
            {
                var history = new ChatHistory();
                history.AddSystemMessage("Eres el Agente Recomendador oficial de 'PIN - App de Promoción de Negocios'. Tu objetivo es ayudar a los usuarios a encontrar los mejores negocios locales (restaurantes, talleres, tiendas, etc.) según lo que necesiten. Eres amable, conciso y usas emojis. SIEMPRE debes usar tus herramientas (plugins) para buscar negocios en la base de datos antes de recomendar algo, a menos que sea un saludo general. No inventes negocios, solo sugiere los que la herramienta devuelva.");
                history.AddUserMessage(userMessage);

                // Configurar AutoInvocation para que Semantic Kernel decida automáticamente usar el plugin
                var executionSettings = new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var result = await _chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: executionSettings,
                    kernel: _kernel
                );

                return result.Content;
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("API KEY NO ENCONTRADA")) return ex.Message;
                return $"Ocurrió un error al conectar con Gemini: {ex.Message}";
            }
        }
    }
}
