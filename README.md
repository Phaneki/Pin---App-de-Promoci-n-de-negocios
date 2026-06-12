# 📍 Pin - Directorio y Promoción de Negocios

**Pin** es una plataforma web integral diseñada para conectar a clientes con negocios locales. Los usuarios pueden explorar establecimientos, dejar reseñas, interactuar con inteligencia artificial para obtener recomendaciones, y registrar sus propios negocios para darlos a conocer.

Este proyecto fue desarrollado implementando buenas prácticas de desarrollo, arquitectura MVC, algoritmos de Machine Learning y un robusto panel de moderación estilo SaaS.

---

## 🔐 Credenciales de Acceso (Evaluación)

Para revisar el **Panel de Moderación** y tener control total sobre la plataforma, inicie sesión con la siguiente cuenta preconfigurada:

- **Correo Electrónico:** `luiscabrera@pin.com`
- **Contraseña:** `12345`
- **Rol:** `MODERADOR`

> **Tip para el evaluador:** Al iniciar sesión con esta cuenta, aparecerá un botón oscuro llamado **"Manager"** en la barra de navegación superior que da acceso al Dashboard Administrativo.

---

## 🚀 Funcionalidades Principales Implementadas

El proyecto se divide en áreas funcionales robustas tanto para el **Cliente (Público)** como para el **Panel de Moderación (Staff)**.

### 1. Sistema de Usuarios y Autenticación
- **Registro y Login tradicional:** Inicio de sesión seguro gestionado por Cookies.
- **Autenticación con Google (OAuth 2.0):** Inicio de sesión con un solo clic usando cuentas de Google.
- **Perfiles Dinámicos:** Panel de "Ajustes de Cuenta", "Mis Favoritos" y "Mis Reseñas".

### 2. Experiencia del Cliente (Directorio Público)
- **Asistente de IA (Semantic Kernel + Gemini):** Un bot integrado en la interfaz que utiliza **Microsoft Semantic Kernel** y la API de **Google Gemini** para buscar negocios en la base de datos y hacer recomendaciones en lenguaje natural.
- **Sistema de Recomendaciones (Machine Learning):** Uso de **ML.NET** para analizar los patrones de búsqueda de los usuarios y sugerir negocios relevantes (Filtrado Colaborativo / Factorización de Matrices).
- **Explorar Negocios:** Buscador integrado con filtros por texto, categoría y distrito.
- **Mapas Interactivos:** Integración con **Leaflet.js** y **OpenStreetMap / Nominatim** para renderizar la ubicación exacta del negocio y calcular rutas.
- **Sistema de Reseñas (Reviews):** Los usuarios pueden calificar negocios (1 a 5 estrellas).

### 3. Panel de Moderación Estilo SaaS (Admin / Manager)
Un panel exclusivo para el Staff (`MODERADOR`), que incluye:
- **Dashboard en Tiempo Real:** Tarjetas KPI dinámicas.
- **Gestión de Negocios:** Aprobación/Rechazo de solicitudes.
- **Directorio de Usuarios:** Gestión de roles y suspensiones.
- **Gestión de Reportes:** Bandeja de entrada para revisar denuncias.
- **Categorías:** Sistema CRUD para agregar o modificar categorías.

---

## 🛠️ Stack Tecnológico

### Backend (Lógica de Servidor)
- **C# & ASP.NET Core 10 MVC:** Framework principal del proyecto.
- **Entity Framework Core 10 (Code-First):** ORM utilizado para la base de datos.
- **PostgreSQL:** Motor de base de datos relacional.
- **Redis (StackExchange.Redis):** Caché Distribuido y gestión persistente de sesiones.
- **Microsoft Semantic Kernel:** Orquestación de IA y plugins.
- **ML.NET:** Modelos de Machine Learning para recomendaciones.
- **CloudinaryDotNet:** Gestión de almacenamiento de imágenes en la nube.

### Frontend (Interfaz de Usuario)
- **HTML5 / CSS3 / Razor Views (`.cshtml`)**
- **Tailwind CSS & Bootstrap 5:** Maquetación moderna híbrida.
- **Vanilla JavaScript:** Para interactividad del cliente (Chat AI, Mapas).
- **Leaflet + Nominatim:** Librerías para mapas y geocoding.

---

## ⚙️ Estructura de la Base de Datos

El sistema utiliza Entity Framework con múltiples tablas y relaciones:
- `Users` / `Usuarios`, `Businesses`, `Categories`, `Reviews`, `Favorites`, `BusinessImages`, `BusinessReports`, `BusquedasUsuario`.

---

## 💻 Cómo ejecutar el proyecto

1. Clonar el repositorio.
2. Asegurarse de tener el **SDK de .NET 10.0** instalado.
3. Restaurar las dependencias ejecutando: `dotnet restore`.
4. Configurar las variables de entorno o el archivo `appsettings.Development.json` con:
   - Cadena de conexión de PostgreSQL.
   - API Key de **Google Gemini** (`Gemini:ApiKey`).
   - Credenciales de **Cloudinary** (si se habilita la subida de fotos).
   - Servidor local de **Redis** en el puerto `6379`.
5. Ejecutar la aplicación con: `dotnet run`.
6. Si la base de datos está vacía, EF Core aplicará las migraciones e inyectará datos semilla automáticamente al inicio.