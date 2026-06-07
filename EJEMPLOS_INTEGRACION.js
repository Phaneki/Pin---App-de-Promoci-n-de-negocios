// ============================================
// EJEMPLOS DE INTEGRACIÓN EN FRONTEND
// ============================================

// 1. REGISTRAR BÚSQUEDA CUANDO USUARIO HACE CLIC
// Agregar en el evento onclick de un negocio

async function registrarBusqueda(negocioId, categoria, zona, usuarioId) {
    try {
        const response = await fetch('/api/recommendations/registrar-busqueda', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                usuarioId: usuarioId,
                negocioId: negocioId,
                categoria: categoria,
                zona: zona,
                tipoInteraccion: 1, // Click
                calificacion: null
            })
        });
        console.log('Búsqueda registrada');
    } catch (error) {
        console.error('Error:', error);
    }
}

// 2. CARGAR RECOMENDACIONES AL ABRIR LA PÁGINA
async function cargarRecomendaciones(usuarioId) {
    try {
        const response = await fetch(`/api/recommendations/personalizadas/${usuarioId}?cantidad=10`);
        const recomendaciones = await response.json();

        // Mostrar en UI
        const container = document.getElementById('recomendaciones');
        container.innerHTML = recomendaciones.map(r => `
            <div class="tarjeta-recomendacion" onclick="registrarBusqueda(${r.negocioId}, '${r.categoria}', 'Centro', ${usuarioId})">
                <img src="${r.imagenUrl}" alt="${r.nombre}">
                <h3>${r.nombre}</h3>
                <p class="categoria">${r.categoria}</p>
                <p class="razon">💡 ${r.razon}</p>
                <div class="rating">
                    <span class="stars">★★★★★ ${r.calificacion}</span>
                    <span class="score">Compatibilidad: ${(r.puntajeRecomendacion * 100).toFixed(0)}%</span>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error cargando recomendaciones:', error);
    }
}

// 3. FILTRAR POR CATEGORÍA
async function recomendacionesPorCategoria(usuarioId, categoria) {
    try {
        const response = await fetch(
            `/api/recommendations/por-categoria/${usuarioId}?categoria=${encodeURIComponent(categoria)}&cantidad=5`
        );
        const recomendaciones = await response.json();
        return recomendaciones;
    } catch (error) {
        console.error('Error:', error);
    }
}

// 4. REGISTRAR CALIFICACIÓN
async function registrarCalificacion(negocioId, calificacion, usuarioId) {
    try {
        const response = await fetch('/api/recommendations/registrar-busqueda', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                usuarioId: usuarioId,
                negocioId: negocioId,
                categoria: document.getElementById(`cat-${negocioId}`).value,
                zona: document.getElementById(`zone-${negocioId}`).value,
                tipoInteraccion: 1,
                calificacion: calificacion // 1-5
            })
        });
        console.log('Calificación registrada:', calificacion);
    } catch (error) {
        console.error('Error:', error);
    }
}

// 5. AGREGAR A FAVORITOS (TipoInteraccion = 2)
async function agregarAFavoritos(negocioId, categoria, zona, usuarioId) {
    try {
        const response = await fetch('/api/recommendations/registrar-busqueda', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                usuarioId: usuarioId,
                negocioId: negocioId,
                categoria: categoria,
                zona: zona,
                tipoInteraccion: 2, // Favorito
                calificacion: null
            })
        });
        console.log('Agregado a favoritos');
    } catch (error) {
        console.error('Error:', error);
    }
}

// 6. HACER RESERVA (TipoInteraccion = 3)
async function hacerReserva(negocioId, categoria, zona, usuarioId) {
    try {
        const response = await fetch('/api/recommendations/registrar-busqueda', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                usuarioId: usuarioId,
                negocioId: negocioId,
                categoria: categoria,
                zona: zona,
                tipoInteraccion: 3, // Reserva
                calificacion: null
            })
        });
        console.log('Reserva registrada');
    } catch (error) {
        console.error('Error:', error);
    }
}

// ============================================
// EJEMPLO HTML
// ============================================

/*
<div class="seccion-recomendaciones">
    <h2>Recomendaciones Personalizadas Para Ti</h2>

    <div id="filtros">
        <button onclick="recomendacionesPorCategoria(userId, 'Restaurantes')">
            🍽️ Restaurantes
        </button>
        <button onclick="recomendacionesPorCategoria(userId, 'Cines')">
            🎬 Cines
        </button>
        <button onclick="recomendacionesPorCategoria(userId, 'Bares')">
            🍺 Bares
        </button>
    </div>

    <div id="recomendaciones" class="grid">
        <!-- Cargadas dinámicamente -->
    </div>
</div>

<script>
// En el documento ready
document.addEventListener('DOMContentLoaded', () => {
    const usuarioId = getCurrentUserId(); // Tu función
    cargarRecomendaciones(usuarioId);
});
</script>
*/

// ============================================
// EJEMPLO C# EN CONTROLADOR
// ============================================

/*
[HttpGet("buscar")]
public async Task<IActionResult> Search(string termino, int usuarioId)
{
    var usuario = await _context.Usuarios.FindAsync(usuarioId);
    var resultados = await _context.Negocios
        .Where(n => n.Nombre.Contains(termino))
        .ToListAsync();

    // Registrar búsqueda
    foreach (var negocio in resultados.Take(1))
    {
        var busqueda = new BusquedaUsuario
        {
            UsuarioId = usuarioId,
            NegocioId = negocio.Id,
            Categoria = negocio.Categoria,
            Zona = ExtractZone(negocio.Direccion),
            FechaBusqueda = DateTime.UtcNow,
            TipoInteraccion = 0 // Solo búsqueda
        };
        _context.BusquedasUsuario.Add(busqueda);
        await _context.SaveChangesAsync();
    }

    return Ok(resultados);
}
*/
