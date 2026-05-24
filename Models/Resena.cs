namespace Pin___App_de_Promoci_n_de_negocios.Models;

public class Resena
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioEmail { get; set; } = string.Empty;
    public int Calificacion { get; set; } // 0-5 estrellas
    public string Comentario { get; set; } = string.Empty;
    public List<string> FotoUrls { get; set; } = new();
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public bool Verificado { get; set; } = false;
}
