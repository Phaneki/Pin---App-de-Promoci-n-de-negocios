namespace PinAppdePromo.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = "CLIENTE"; //  IMPORTANTE
    }
}