namespace Pin___App_de_Promoci_n_de_negocios.Models;
public class Negocio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public double Calificacion { get; set; }
    public bool EstaAbierto { get; set; }
    public bool Destacado { get; set; }
    public string ImagenUrl { get; set; } = "~/images/picanteria.jpg";
    public string Descripcion { get; set; } = string.Empty;
    public string Horarios { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string SitioWeb { get; set; } = string.Empty;
}
