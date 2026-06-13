# 📸 Guía de Configuración: Google Places API para Imágenes de Negocios

## ¿Qué hace esta integración?

La aplicación ahora puede obtener automáticamente fotos de negocios desde Google Places API. Esto significa que:

✅ Cuando un negocio no tiene imagen, se obtiene automáticamente de Google  
✅ Las imágenes se guardan en la base de datos para futuras consultas  
✅ Si algo falla, se usa un avatar colorido como fallback  
✅ Compatible con Render (Linux) y Windows

---

## Paso 1: Obtener la Google Places API Key

### 1.1 Crear un Proyecto en Google Cloud Console

1. Ve a [Google Cloud Console](https://console.cloud.google.com/)
2. Si no tienes cuenta, crea una (es gratis, pero necesitas tarjeta de crédito para verificación)
3. Haz clic en el selector de proyecto (arriba a la izquierda)
4. Selecciona **"Nuevo Proyecto"**
5. Dale un nombre como: `PIN-App-Places-Photos`
6. Espera a que se cree el proyecto (toma unos segundos)

### 1.2 Habilitar la Google Places API

1. En el menú de la izquierda, selecciona **"APIs y servicios"**
2. Haz clic en **"Biblioteca"**
3. Busca: `Places API`
4. Haz clic en el resultado y después en **"HABILITAR"**

También necesitas habilitar estas APIs adicionales:

- **Maps JavaScript API** (para que funcione completamente)
- **Geocoding API** (para búsquedas de ubicaciones)

### 1.3 Crear Credenciales (API Key)

1. Ve a **"APIs y servicios"** > **"Credenciales"**
2. Haz clic en **"+ Crear credenciales"** (arriba)
3. Selecciona **"Clave de API"**
4. Se generará automáticamente tu clave (algo como: `AIzaSyD...abc123xyz`)
5. **Cópiala** (aparecerá un botón de copiar)

### 1.4 Restringir la clave (Recomendado para producción)

1. En la página de credenciales, haz clic en tu clave recién creada
2. En **"Restricción de aplicación"**, selecciona **"Aplicaciones HTTP"**
3. En **"Sitios web autorizados"**, agrega:
   - `https://pin-app-de-promoci-n-de-negocios.onrender.com` (producción)
   - `http://localhost:5000` (desarrollo local)
4. En **"Restricción de API"**, selecciona **"Places API"**
5. Guarda los cambios

---

## Paso 2: Configurar la API Key en tu Aplicación

### 2.1 Configuración LOCAL (Development)

Abre `appsettings.Development.json` (o `appsettings.json` si lo prefieres):

```json
{
  "GoogleMaps": {
    "ApiKey": "AIzaSyD...tu_api_key_aqui"
  }
}
```

### 2.2 Configuración en RENDER (Production)

1. Ve a tu dashboard de Render: https://dashboard.render.com/
2. Selecciona tu aplicación PIN
3. Ve a la sección **"Environment"**
4. Haz clic en **"Add Environment Variable"**
5. Agrega:
   - **Key**: `GOOGLE_MAPS_API_KEY`
   - **Value**: `AIzaSyD...tu_api_key_aqui`
6. Guarda y redeploy la aplicación

**IMPORTANTE**: Actualiza también el código de Render para que lea esta variable:

En `Program.cs`, la aplicación intenta leer:

```csharp
var apiKey = builder.Configuration["GoogleMaps:ApiKey"];
```

Si usas variable de entorno, debes cambiar a:

```csharp
var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")
             ?? builder.Configuration["GoogleMaps:ApiKey"];
```

---

## Paso 3: Sincronizar Imágenes de Negocios

### Opción A: Sincronizar TODO (Todos los negocios)

Abre tu navegador y ve a:

```
https://tu-app.onrender.com/api/businessimages/sync-all
```

Será una petición POST, así que usa una herramienta como **Postman** o **Insomnia**:

**URL**: `POST https://tu-app.onrender.com/api/businessimages/sync-all`

**Respuesta exitosa**:

```json
{
  "message": "✅ Sincronización completada",
  "successful": 5,
  "failed": 0,
  "total": 5
}
```

### Opción B: Sincronizar UN negocio específico

**URL**: `POST https://tu-app.onrender.com/api/businessimages/sync/1`
(Cambia `1` por el ID del negocio)

**Respuesta**:

```json
{
  "message": "✅ Imagen sincronizada correctamente",
  "imageUrl": "https://maps.googleapis.com/maps/api/place/photo?..."
}
```

### Opción C: Vista previa (Ver qué imagen se obtendría)

**URL**: `GET https://tu-app.onrender.com/api/businessimages/preview/1`

No guarda nada, solo muestra qué imagen se obtendría.

---

## Paso 4: Verificar que funciona

### 4.1 En LOCAL

1. Abre la terminal en la carpeta del proyecto
2. Ejecuta: `dotnet run`
3. Ve a `http://localhost:5000/Home/Explorar`
4. Las imágenes ahora deberían cargar

### 4.2 En RENDER

1. Abre tu app en Render
2. Ve a la sección **"Logs"** y busca mensajes de error
3. Si ves `⚠️ Google Places API Key no está configurada`, verifica el paso 2.2

---

## Solución de Problemas

### ❌ Error: "API key not valid"

**Causa**: La clave es inválida o expiró  
**Solución**: Crea una nueva clave en Google Cloud Console

### ❌ Error: "REQUEST_DENIED"

**Causa**: Places API no está habilitada  
**Solución**: Ve a APIs > Biblioteca > Busca "Places API" > Habilitar

### ❌ Error: "INVALID_REQUEST"

**Causa**: El query (búsqueda) es vacío o el negocio no existe en Google  
**Solución**: El sistema automáticamente usa un fallback (avatar colorido)

### ⚠️ Las imágenes no cargan en RENDER pero sí en LOCAL

**Causa**: Variable de entorno no está configurada correctamente  
**Solución**: Asegúrate de seguir exactamente el paso 2.2

---

## Costo de Google Places API

- **Primeros 200 USD**: Gratis cada mes (crédito)
- Cada búsqueda de texto: $0.0032 USD
- Cada solicitud de detalles: $0.015 USD
- Cada solicitud de foto: $0.007 USD

**Para tu aplicación**: Sincronizar 100 negocios te costaría ~$2.40 USD

Los créditos gratuitos cubren fácilmente esto.

---

## Endpoints Disponibles

| Método | Endpoint                                   | Descripción                                |
| ------ | ------------------------------------------ | ------------------------------------------ |
| POST   | `/api/businessimages/sync-all`             | Sincroniza TODOS los negocios sin imágenes |
| POST   | `/api/businessimages/sync/{businessId}`    | Sincroniza un negocio específico           |
| GET    | `/api/businessimages/preview/{businessId}` | Vista previa de imagen (sin guardar)       |
| DELETE | `/api/businessimages/{imageId}`            | Elimina una imagen                         |

---

## Siguiente Paso

Después de configurar la API, ejecuta en la terminal:

```bash
# Para sincronizar todo en LOCAL
curl -X POST http://localhost:5000/api/businessimages/sync-all

# Para sincronizar todo en RENDER (reemplaza con tu URL)
curl -X POST https://tu-app.onrender.com/api/businessimages/sync-all
```

---

## ¿Preguntas?

Si algo no funciona:

1. Verifica los logs de tu aplicación (`dotnet run` muestra los errores)
2. Comprueba que la API key esté correctamente guardada
3. Asegúrate de que Places API esté habilitada en Google Cloud Console
