using System.Collections.Generic;

namespace PinAppdePromo.Models
{
    public class ModeracionViewModel
    {
        public int SolicitudesPendientes { get; set; }
        public int NegociosAprobadosHoy { get; set; }
        public double TasaRechazo { get; set; }
        public List<BusinessReport> DenunciasPendientes { get; set; } = new List<BusinessReport>();
        public List<StaffLog> ActividadReciente { get; set; } = new List<StaffLog>();
    }
}