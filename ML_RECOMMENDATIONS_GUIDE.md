# 🤖 Sistema de Recomendaciones Personalizadas con ML.NET

## 📋 Descripción General

Este sistema implementa **Machine Learning con ML.NET** para generar recomendaciones personalizadas de negocios basadas en:

✅ **Búsquedas anteriores** del usuario por categoría  
✅ **Zona geográfica** donde normalmente busca o se ubica  
✅ **Patrones de comportamiento** (clicks, favoritos, reservas)  
✅ **Calificaciones** dadas a negocios  

---

## 🏗️ Arquitectura del Sistema

```
┌─────────────────────────────────────────┐
│     Controlador API (Recommendations)    │
│     - /api/recommendations/personalizadas
│     - /api/recommendations/por-categoria
│     - /api/recommendations/historial     │
└────────────┬────────────────────────────┘
			 │
			 ▼
┌─────────────────────────────────────────┐
│   RecommendationAnalysisService         │
│   - Analiza patrones                    │
│   - Genera razones explicables          │
│   - Controla lógica de negocio          │
└────────────┬────────────────────────────┘
			 │
			 ▼
┌─────────────────────────────────────────┐
│     RecommendationService (ML.NET)      │
│     - Entrena modelo                    │
│     - Predice scores                    │
│     - Gestiona modelo entrenado         │
└────────────┬────────────────────────────┘
			 │
			 ▼
┌─────────────────────────────────────────┐
│    Datos: BusquedaUsuario (BD)          │
│    - Historial de búsquedas             │
│    - Interacciones                      │
│    - Calificaciones                     │
└─────────────────────────────────────────┘
```

---

## 📦 Componentes Creados

### 1. **Modelos de Datos** (`Models/`)

#### `BusquedaUsuario.cs`
Registra cada búsqueda e interacción del usuario:

```csharp
public class BusquedaUsuario
{
	public int Id { get; set; }
	public int UsuarioId { get; set; }
	public int NegocioId { get; set; }
	public string Categoria { get; set; }      // "Restaurantes", "Cines", etc.
	public string Zona { get; set; }           // "Centro", "Miraflores", etc.
	public DateTime FechaBusqueda { get; set; }
	public int TipoInteraccion { get; set; }   // 0=búsqueda, 1=click, 2=favorito, 3=reserva
	public float? Calificacion { get; set; }   // 1-5 estrellas
}
```

### 2. **Modelos de ML** (`ML/`)

#### `RecommendationModels.cs`
Define entrada/salida del modelo ML.NET:

- **RecomendacionInput**: Features (características) para predicción
- **RecomendacionOutput**: Resultado (Score de recomendación)
- **RecomendacionTraining**: Incluye etiqueta para entrenamiento

#### `RecommendationService.cs`
**Motor ML.NET** que:
- ✅ Entrena modelos de clasificación binaria
- ✅ Predice si un negocio debe recomendarse
- ✅ Guarda/carga modelos desde disco
- ✅ Maneja 7 características:
  1. ID del usuario
  2. ID del negocio
  3. Hash de categoría
  4. Hash de zona
  5. Frecuencia de categoría
  6. Frecuencia de zona
  7. Calificación promedio

#### `RecommendationAnalysisService.cs`
**Orquesta** el análisis:
- Calcula frecuencias de búsqueda
- Genera razones explicables
- Filtra recomendaciones por categoría
- Controla entrenamientos periódicos

### 3. **Controlador API** (`Controllers/RecommendationsController.cs`)

```http
GET  /api/recommendations/personalizadas/{usuarioId}
GET  /api/recommendations/por-categoria/{usuarioId}?categoria=Restaurantes
GET  /api/recommendations/historial/{usuarioId}
POST /api/recommendations/registrar-busqueda
```

---

## 🚀 Cómo Usar

### 1. **Registrar una Búsqueda**

Cada vez que un usuario busca un negocio, registra con:

```bash
curl -X POST https://tu-api.com/api/recommendations/registrar-busqueda \
  -H "Content-Type: application/json" \
  -d '{
	"usuarioId": 1,
	"negocioId": 42,
	"categoria": "Restaurantes",
	"zona": "Centro",
	"tipoInteraccion": 1,
	"calificacion": 4.5
  }'
```

**TipoInteraccion:**
- `0`: Solo búsqueda
- `1`: Click en negocio
- `2`: Agregado a favoritos
- `3`: Hizo una reserva

### 2. **Obtener Recomendaciones Personalizadas**

```bash
curl https://tu-api.com/api/recommendations/personalizadas/1?cantidad=10
```

**Respuesta:**
```json
[
  {
	"negocioId": 42,
	"nombre": "Cevichería Marina",
	"categoria": "Restaurantes",
	"calificacion": 4.8,
	"puntajeRecomendacion": 0.87,
	"razon": "Te gusta la categoría Restaurantes • Tiene excelente calificación (4.8/5)",
	"imagenUrl": "https://..."
  },
  {
	"negocioId": 15,
	"nombre": "Cinemark",
	"categoria": "Cines",
	"calificacion": 4.2,
	"puntajeRecomendacion": 0.65,
	"razon": "Recomendación basada en tu perfil",
	"imagenUrl": "https://..."
  }
]
```

### 3. **Recomendaciones por Categoría**

```bash
curl "https://tu-api.com/api/recommendations/por-categoria/1?categoria=Restaurantes&cantidad=5"
```

### 4. **Ver Historial de Búsquedas**

```bash
curl https://tu-api.com/api/recommendations/historial/1?dias=30
```

---

## 🧠 Cómo Funciona el Modelo ML

### **Algoritmo: Regresión Logística**

El sistema usa **Logistic Regression** (Regresión Logística) para clasificación binaria:

```
Input Features (7)
		↓
	[ML.NET Pipeline]
		↓
Logistic Regression Model
		↓
Score de Recomendación (0-1)
```

### **Características (Features)**

| Feature | Descripción | Ejemplo |
|---------|-----------|---------|
| `UsuarioId` | ID normalizado | 1, 2, 3... |
| `NegocioId` | ID del negocio | 42, 100... |
| `CategoriaHash` | Hash de categoría | 0.15-0.95 |
| `ZonaHash` | Hash de zona | 0.20-0.80 |
| `FrecuenciaCategoria` | Veces que buscó categoría | 0, 5, 10... |
| `FrecuenciaZona` | Veces que buscó en zona | 0, 3, 8... |
| `CalificacionPromedio` | Promedio de sus calificaciones | 3.5, 4.2... |

### **Etiqueta (Label)**

```
Label = 1  ➜  RECOMENDAR (interacción positiva: click, favorito, reserva)
Label = 0  ➜  NO RECOMENDAR (solo búsqueda, sin acción)
```

### **Entrenamiento Periódico**

- Se entrena automáticamente cada **50 nuevas búsquedas**
- Mínimo 10 registros para entrenar
- Modelo se guarda en: `Models/recommendation_model.zip`

---

## 🔧 Instalación y Configuración

### 1. **El sistema ya está integrado**, pero para verificar:

```bash
# Restaurar paquetes NuGet
dotnet restore

# Crear migración (ya está lista)
dotnet ef migrations add AddBusquedaUsuarioTable --context PinDbContext

# Aplicar migración a BD
dotnet ef database update --context PinDbContext
```

### 2. **En `Program.cs` (ya configurado):**

```csharp
using PinAppdePromo.ML;

// ... resto del código ...

// Registrar servicios de ML
builder.Services.AddMachineLearningServices();
```

### 3. **Inyección de Dependencias en Controladores:**

```csharp
public class TuControlador : Controller
{
	private readonly RecommendationAnalysisService _recommendations;

	public TuControlador(RecommendationAnalysisService recommendations)
	{
		_recommendations = recommendations;
	}
}
```

---

## 📊 Casos de Uso

### **Caso 1: Usuario busca Restaurantes en Centro**
```
Usuario → Busca "Restaurantes en Centro"
Sistema → Registra búsqueda (TipoInteraccion=0)
Usuario → Hace clic en "Cevichería Marina"
Sistema → Actualiza a TipoInteraccion=1
...después de 50 búsquedas...
Sistema → Entrena modelo
Sistema → En futuras búsquedas:
		  ✅ Prioriza Restaurantes en Centro
		  ✅ Recommienda negocios similares
```

### **Caso 2: Recomendaciones según ubicación**
```
Usuario "Pérez" busca en: "Miraflores" (60%), "Centro" (30%), "Barranco" (10%)
Sistema → Prioriza negocios en Miraflores
Sistema → Luego sugiere Centro
Sistema → Finalmente Barranco
```

### **Caso 3: Mejorar recomendaciones con calificaciones**
```
Usuario califica restaurantes ≥4 estrellas → Sistema aprende su gusto
Sistema → Enfatiza negocios con similares características
```

---

## 🎯 Métricas y Monitoreo

### **Posibles Mejoras Futuras**

1. **Usar Matriz de Recomendación (Factorización)**
   ```csharp
   MatrixFactorizationTrainer.MatrixFactorization(...)
   ```

2. **Incorporar Reviews/Reseñas**
   ```csharp
   // Análisis de sentimiento de reviews
   ```

3. **A/B Testing**
   ```csharp
   // Comparar dos versiones del modelo
   ```

4. **Feedback Loop**
   ```csharp
   // Usuario indica si recomendación fue útil
   ```

---

## 🐛 Solución de Problemas

| Problema | Solución |
|----------|----------|
| El modelo no entrena | Verificar mínimo 10 búsquedas |
| Las recomendaciones son genéricas | El modelo necesita más datos de entrenamiento |
| Lentitud en primeras llamadas | Normal: se carga modelo de disco |
| Carpeta `Models/` no existe | El sistema la crea automáticamente |

---

## 📚 Referencia: APIs Disponibles

### **GET** `/api/recommendations/personalizadas/{usuarioId}`
Obtiene top 10 (personalizable) recomendaciones.

**Parámetros:**
- `usuarioId` (path): ID del usuario
- `cantidad` (query): 1-20 (default: 10)

**Respuesta:** Array de `RecommendationResponse`

---

### **GET** `/api/recommendations/por-categoria/{usuarioId}`
Filtra recomendaciones por categoría específica.

**Parámetros:**
- `usuarioId` (path): ID del usuario
- `categoria` (query): "Restaurantes", "Cines", etc.
- `cantidad` (query): 1-20 (default: 10)

---

### **GET** `/api/recommendations/historial/{usuarioId}`
Ver todas las búsquedas del usuario en los últimos N días.

**Parámetros:**
- `usuarioId` (path): ID del usuario
- `dias` (query): Número de días (default: 30)

---

### **POST** `/api/recommendations/registrar-busqueda`
Registra interacción del usuario con un negocio.

**Body:**
```json
{
  "usuarioId": 1,
  "negocioId": 42,
  "categoria": "Restaurantes",
  "zona": "Centro",
  "tipoInteraccion": 1,
  "calificacion": 4.5
}
```

---

## 🎓 Conceptos Clave

### **Regresión Logística**
Algoritmo de clasificación que predice probabilidad de una clase (0 o 1).
- ✅ Rápido de entrenar
- ✅ Interpretable
- ✅ Ideal para comenzar

### **Features (Características)**
Entrada numérica que el modelo usa para aprender patrones.

### **Label (Etiqueta)**
Valor verdadero (0 o 1) que usamos en entrenamiento.

### **Pipeline**
Secuencia de transformaciones + modelo ML.

### **Predicción**
Resultado final entre 0 y 1 (probabilidad).

---

## 📝 Notas

- El modelo se **reentrana cada 50 búsquedas**
- Se guarda en `Models/recommendation_model.zip`
- Si no existe, se crea un modelo por defecto
- **Escalable**: Agregar más features es simple
- **Seguro**: Solo ve datos del usuario en sesión

---

¡Listo para usar! 🚀
