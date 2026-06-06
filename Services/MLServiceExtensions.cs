using PinAppdePromo.ML;

namespace PinAppdePromo.Services;

/// <summary>
/// Extensión para registrar servicios de ML en el contenedor de inyección de dependencias
/// </summary>
public static class MLServiceExtensions
{
    public static IServiceCollection AddMachineLearningServices(
        this IServiceCollection services,
        string modelPath = "Models/recommendation_model.zip")
    {
        // Registrar RecommendationService como singleton (el modelo se carga una vez)
        services.AddSingleton(new RecommendationService(modelPath));

        // Registrar RecommendationAnalysisService como transient
        services.AddTransient<RecommendationAnalysisService>();

        return services;
    }
}
