namespace PinAppdePromo.Models;

/// <summary>
/// Modelo para registrar búsquedas e interacciones del usuario con negocios
/// </summary>
public class BusquedaUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int NegocioId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public DateTime FechaBusqueda { get; set; }

    /// <summary>
    /// Tipo de interacción: 0=búsqueda, 1=click, 2=favorito, 3=reserva
    /// </summary>
    public int TipoInteraccion { get; set; }

    /// <summary>
    /// Puntuación dada por el usuario (1-5)
    /// </summary>
    public float? Calificacion { get; set; }

    public User? Usuario { get; set; }
    public Business? Negocio { get; set; }
}
