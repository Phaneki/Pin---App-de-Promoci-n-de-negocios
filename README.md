# 📍 Pin - Directorio y Promoción de Negocios

**Pin** es una plataforma web integral diseñada para conectar a clientes con negocios locales. Los usuarios pueden explorar establecimientos, dejar reseñas, guardar favoritos y, si lo desean, registrar sus propios negocios para darlos a conocer.

Este proyecto fue desarrollado como parte del curso de **Programación I**, implementando buenas prácticas de desarrollo, arquitectura MVC y un robusto panel de moderación estilo SaaS.

---

## 🔐 Credenciales de Acceso (Evaluación)

Para revisar el **Panel de Moderación** y tener control total sobre la plataforma, inicie sesión con la siguiente cuenta preconfigurada:

- **Correo Electrónico:** `luiscabrera@pin.com`
- **Contraseña:** `12345` 
- **Rol:** `MODERADOR`

> **Tip para el evaluador:** Al iniciar sesión con esta cuenta, aparecerá un botón rojo llamado **"Manager"** en la barra de navegación superior que da acceso al Dashboard Administrativo.

---

## 🚀 Funcionalidades Principales Implementadas

El proyecto se divide en dos grandes áreas funcionales: **El Lado del Cliente (Público)** y el **Panel de Moderación (Staff)**.

### 1. Sistema de Usuarios y Autenticación

- **Registro y Login tradicional:** Inicio de sesión seguro gestionado por Cookies y Sesiones.
- **Autenticación con Google (OAuth 2.0):** Los usuarios pueden iniciar sesión o registrarse con un solo clic usando su cuenta de Google.
- **Perfiles Dinámicos:** Panel de "Ajustes de Cuenta", "Mis Favoritos" y "Mis Reseñas".
- **Restricción de Acceso:** Usuarios con rol `SUSPENDIDO` pierden acceso a la plataforma.

### 2. Experiencia del Cliente (Directorio Público)

- **Explorar Negocios:** Buscador integrado con filtros por texto, categoría y distrito.
- **Visualización de Negocios:** Páginas de detalle generadas dinámicamente con información de contacto, fotos y horarios.
- **Mapas Interactivos:** Integración con **Leaflet.js** y **OpenStreetMap** para renderizar la ubicación exacta del negocio y calcular rutas.
- **Sistema de Reseñas (Reviews):** Los usuarios autenticados pueden calificar negocios (1 a 5 estrellas) y dejar comentarios en tiempo real mediante modales dinámicos.
- **Registro de Negocios:** Formulario interactivo donde los usuarios colocan un "Pin" en el mapa para registrar la latitud y longitud exacta de su local.

### 3. Panel de Moderación Estilo SaaS (Admin / Manager)

Un panel oscuro y moderno exclusivo para el Staff (`MODERADOR`), que incluye:

- **Dashboard en Tiempo Real:** Tarjetas KPI dinámicas que calculan métricas como "Solicitudes de hoy", "Tasa de rechazo" y rastrean la actividad reciente de los moderadores (`StaffLogs`).
- **Gestión de Negocios (`NegociosAdmin`):**
  - Listado paginado de negocios con buscador.
  - Sistema de **Aprobación/Rechazo**. Al aprobar un negocio, el sistema automáticamente asciende al usuario creador al rol de `DUEÑO`.
- **Directorio de Usuarios (`UsuariosAdmin`):**
  - Filtros avanzados por rol (`CLIENTE`, `DUEÑO`, `MODERADOR`, `SUSPENDIDO`).
  - Botones seguros para ascender usuarios a moderadores o bloquear cuentas tóxicas.
- **Gestión de Reportes (`ReportesAdmin`):**
  - Bandeja de entrada donde el Staff revisa denuncias de la comunidad. Permite descartar la denuncia o sancionar/ocultar el negocio infractor con un clic.
- **Categorías (`CategoriasAdmin`):** Sistema CRUD con edición "in-line" para agregar o modificar categorías del sistema.

---

## 🛠️ Stack Tecnológico

### Backend (Lógica de Servidor)

- **C# & ASP.NET Core 9 MVC:** Framework principal del proyecto.
- **Entity Framework Core (Code-First):** ORM utilizado para la gestión y modelado de la base de datos.
- **PostgreSQL:** Motor de base de datos relacional (alojado en la nube vía Render).
- **Redis (StackExchange.Redis):** Implementado para el Caché Distribuido y la gestión persistente de sesiones.

### Frontend (Interfaz de Usuario)

- **HTML5 / CSS3 / Razor Views (`.cshtml`)**
- **Tailwind CSS:** Utilizado intensivamente para la maquetación del Dashboard Administrativo (SaaS UI).
- **Bootstrap 5:** Sistema de grillas y componentes para el portal público.
- **Vanilla JavaScript:** Para el manejo dinámico de DOM (Modales, edición de tablas y vista previa de imágenes).
- **Leaflet + Nominatim (Geocoding):** Librerías para mapas y búsqueda de direcciones.
- **FontAwesome 6:** Iconografía en todo el sitio.

---

## ⚙️ Estructura de la Base de Datos

El sistema utiliza Entity Framework con múltiples relaciones (`1:N` y `N:M`):

- `Usuarios` / `Users`: Gestión de identidades y roles.
- `Businesses`: Tabla central de negocios (relacionada con el Dueño y Categoría).
- `Categories`: Clasificación de los negocios.
- `Reviews`: Sistema de calificaciones.
- `Favorites`: Relación usuario-negocio guardado.
- `BusinessImages`: Galería fotográfica.
- `BusinessReports`: Manejo de denuncias y moderación.
- `StaffLogs`: Historial de auditoría para los moderadores.

---

## 💻 Cómo ejecutar el proyecto

1. Clonar el repositorio.
2. Asegurarse de tener el **SDK de .NET 9.0** instalado.
3. Restaurar las dependencias ejecutando: `dotnet restore`.
4. _(Opcional)_ Levantar un servidor local de **Redis** en el puerto `6379`.
5. Ejecutar la aplicación con: `dotnet run`.
6. Si la base de datos está vacía, puedes poblarla navegando a la ruta oculta `/Home/GenerarDatosDePrueba` para inyectar usuarios y negocios iniciales.
