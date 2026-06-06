using Microsoft.ML;
using PinAppdePromo.Models;

namespace PinAppdePromo.ML;

/// <summary>
/// Servicio de Machine Learning para generar recomendaciones personalizadas
/// basadas en búsquedas por categoría y zona del usuario
/// </summary>
public class RecommendationService
{
    private readonly MLContext _mlContext;
    private ITransformer? _trainedModel;
    private PredictionEngine<RecomendacionInput, RecomendacionOutput>? _predictionEngine;
    private readonly string _modelPath;

    public RecommendationService(string modelPath = "Models/recommendation_model.zip")
    {
        _mlContext = new MLContext(seed: 42);
        _modelPath = modelPath;
        LoadOrCreateModel();
    }

    /// <summary>
    /// Carga el modelo entrenado desde disco o lo crea si no existe
    /// </summary>
    private void LoadOrCreateModel()
    {
        if (File.Exists(_modelPath))
        {
            try
            {
                using (var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read))
                {
                    _trainedModel = _mlContext.Model.Load(stream, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<RecomendacionInput, RecomendacionOutput>(_trainedModel, ignoreMissingColumns: true);
                }
            }
            catch
            {
                // Si hay error al cargar, crear modelo por defecto
                CreateDefaultModel();
            }
        }
        else
        {
            CreateDefaultModel();
        }
    }

    /// <summary>
    /// Crea un modelo por defecto si no existe datos de entrenamiento
    /// </summary>
    private void CreateDefaultModel()
    {
        // Crear datos semilla para evitar que el entrenamiento falle con 0 filas
        var seedData = new List<RecomendacionTraining>
        {
            new RecomendacionTraining { UsuarioId = "1", NegocioId = "1", Categoria = "Restaurantes", Zona = "Centro", FrecuenciaCategoria = 3, FrecuenciaZona = 5, CalificacionPromedio = 4.5f, Label = true },
            new RecomendacionTraining { UsuarioId = "1", NegocioId = "2", Categoria = "Cines", Zona = "Centro", FrecuenciaCategoria = 1, FrecuenciaZona = 2, CalificacionPromedio = 3.0f, Label = false },
            new RecomendacionTraining { UsuarioId = "2", NegocioId = "3", Categoria = "Restaurantes", Zona = "Miraflores", FrecuenciaCategoria = 2, FrecuenciaZona = 4, CalificacionPromedio = 4.0f, Label = true },
            new RecomendacionTraining { UsuarioId = "2", NegocioId = "4", Categoria = "Tecnología", Zona = "San Isidro", FrecuenciaCategoria = 1, FrecuenciaZona = 1, CalificacionPromedio = 2.5f, Label = false },
            new RecomendacionTraining { UsuarioId = "3", NegocioId = "5", Categoria = "Salud", Zona = "Surco", FrecuenciaCategoria = 1, FrecuenciaZona = 1, CalificacionPromedio = 5.0f, Label = true }
        };

        var dataView = _mlContext.Data.LoadFromEnumerable(seedData);

        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(new[] {
                new InputOutputColumnPair("UsuarioIdEncoded", nameof(RecomendacionTraining.UsuarioId)),
                new InputOutputColumnPair("NegocioIdEncoded", nameof(RecomendacionTraining.NegocioId)),
                new InputOutputColumnPair("CategoriaEncoded", nameof(RecomendacionTraining.Categoria)),
                new InputOutputColumnPair("ZonaEncoded", nameof(RecomendacionTraining.Zona))
            })
            .Append(_mlContext.Transforms.Concatenate("Features",
                "UsuarioIdEncoded", "NegocioIdEncoded", "CategoriaEncoded", "ZonaEncoded",
                nameof(RecomendacionTraining.FrecuenciaCategoria),
                nameof(RecomendacionTraining.FrecuenciaZona),
                nameof(RecomendacionTraining.CalificacionPromedio)))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

        _trainedModel = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<RecomendacionInput, RecomendacionOutput>(_trainedModel, ignoreMissingColumns: true);
        SaveModel();
    }

    /// <summary>
    /// Entrena el modelo con datos históricos de búsquedas
    /// </summary>
    public void TrainModel(List<BusquedaUsuario> busquedas)
    {
        if (busquedas.Count < 10)
        {
            return; // Necesitamos mínimo 10 registros
        }

        // Generar datos de entrenamiento desde búsquedas
        var trainingData = GenerateTrainingData(busquedas);
        if (trainingData == null || trainingData.Count == 0)
        {
            return; // No hay datos útiles para entrenar
        }
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(new[] {
                new InputOutputColumnPair("UsuarioIdEncoded", nameof(RecomendacionTraining.UsuarioId)),
                new InputOutputColumnPair("NegocioIdEncoded", nameof(RecomendacionTraining.NegocioId)),
                new InputOutputColumnPair("CategoriaEncoded", nameof(RecomendacionTraining.Categoria)),
                new InputOutputColumnPair("ZonaEncoded", nameof(RecomendacionTraining.Zona))
            })
            .Append(_mlContext.Transforms.Concatenate("Features",
                "UsuarioIdEncoded", "NegocioIdEncoded", "CategoriaEncoded", "ZonaEncoded",
                nameof(RecomendacionTraining.FrecuenciaCategoria),
                nameof(RecomendacionTraining.FrecuenciaZona),
                nameof(RecomendacionTraining.CalificacionPromedio)))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

        _trainedModel = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<RecomendacionInput, RecomendacionOutput>(_trainedModel, ignoreMissingColumns: true);
        SaveModel();
    }

    /// <summary>
    /// Predice si un negocio debe ser recomendado a un usuario
    /// </summary>
    public float PredictRecommendationScore(int usuarioId, int negocioId, string categoria, string zona,
        Dictionary<string, int> frecuenciasCategorias, Dictionary<string, int> frecuenciasZonas,
        double calificacionPromedio)
    {
        if (_predictionEngine == null)
        {
            return 0f;
        }

        var input = new RecomendacionInput
        {
            UsuarioId = usuarioId.ToString(),
            NegocioId = negocioId.ToString(),
            Categoria = categoria ?? string.Empty,
            Zona = zona ?? string.Empty,
            FrecuenciaCategoria = frecuenciasCategorias.ContainsKey(categoria) ? frecuenciasCategorias[categoria] : 0,
            FrecuenciaZona = frecuenciasZonas.ContainsKey(zona) ? frecuenciasZonas[zona] : 0,
            CalificacionPromedio = (float)calificacionPromedio
        };

        var prediction = _predictionEngine.Predict(input);
        return prediction.Probability;
    }

    /// <summary>
    /// Genera datos de entrenamiento desde búsquedas históricas
    /// </summary>
    private List<RecomendacionTraining> GenerateTrainingData(List<BusquedaUsuario> busquedas)
    {
        var trainingData = new List<RecomendacionTraining>();

        // Agrupar búsquedas por usuario
        var busquedasPorUsuario = busquedas.GroupBy(b => b.UsuarioId);

        foreach (var grupo in busquedasPorUsuario)
        {
            var usuarioId = grupo.Key;
            var busquedasUsuario = grupo.ToList();

            // Calcular frecuencias
            var frecuenciasCategorias = busquedasUsuario
                .GroupBy(b => b.Categoria)
                .ToDictionary(g => g.Key, g => g.Count());

            var frecuenciasZonas = busquedasUsuario
                .GroupBy(b => b.Zona)
                .ToDictionary(g => g.Key, g => g.Count());

            // Crear datos de entrenamiento para cada búsqueda
            foreach (var busqueda in busquedasUsuario)
            {
                var calificacionPromedio = busquedasUsuario
                    .Where(b => b.Calificacion.HasValue)
                    .Average(b => b.Calificacion ?? 0);

                // Etiqueta: recomendar si fue interacción positiva (click, favorito, reserva)
                var label = busqueda.TipoInteraccion >= 1 ? true : false;

                trainingData.Add(new RecomendacionTraining
                {
                    UsuarioId = usuarioId.ToString(),
                    NegocioId = busqueda.NegocioId.ToString(),
                    Categoria = busqueda.Categoria ?? string.Empty,
                    Zona = busqueda.Zona ?? string.Empty,
                    FrecuenciaCategoria = frecuenciasCategorias.ContainsKey(busqueda.Categoria) ? frecuenciasCategorias[busqueda.Categoria] : 0,
                    FrecuenciaZona = frecuenciasZonas.ContainsKey(busqueda.Zona) ? frecuenciasZonas[busqueda.Zona] : 0,
                    CalificacionPromedio = (float)calificacionPromedio,
                    Label = label
                });
            }
        }

        return trainingData;
    }

    /// <summary>
    /// Guarda el modelo entrenado en disco
    /// </summary>
    private void SaveModel()
    {
        if (_trainedModel != null)
        {
            var directory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write))
            {
                _mlContext.Model.Save(_trainedModel, null, stream);
            }
        }
    }
}
