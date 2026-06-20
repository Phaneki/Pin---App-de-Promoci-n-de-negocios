using Microsoft.SemanticKernel;
using PinAppdePromo.Models;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PinAppdePromo.Plugins
{
    public class BusinessSearchPlugin
    {
        private readonly PinDbContext _context;

        public BusinessSearchPlugin(PinDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("Busca negocios en la base de datos por categoría, nombre o descripción. Devuelve una lista de nombres y detalles. Usa esta herramienta cuando el usuario pida recomendaciones de lugares.")]
        public async Task<string> BuscarNegociosAsync(
            [Description("El término de búsqueda (ej. 'ceviche', 'pizza', 'taller')")] string busqueda)
        {
            var query = _context.Businesses
                .Include(b => b.Category)
                .Where(b => b.Status == "Approved");

            if (!string.IsNullOrEmpty(busqueda))
            {
                var busquedaLower = busqueda.ToLower();
                query = query.Where(b => 
                    b.TradeName.ToLower().Contains(busquedaLower) || 
                    b.Description.ToLower().Contains(busquedaLower) ||
                    (b.Category != null && b.Category.Name.ToLower().Contains(busquedaLower)));
            }

            var resultados = await query.Take(5).ToListAsync();

            if (!resultados.Any())
            {
                return $"No encontré negocios que coincidan con la búsqueda: '{busqueda}'.";
            }

            var respuesta = "Encontré estos negocios:\n";
            foreach (var neg in resultados)
            {
                respuesta += $"- {neg.TradeName} ({neg.Category?.Name}). Dirección: {neg.Address}. Descripción: {neg.Description}. Teléfono: {neg.ContactPhone}\n";
            }

            return respuesta;
        }
        
        [KernelFunction, Description("Verifica si hay negocios abiertos o qué horarios tienen. Útil cuando el usuario quiere saber si algo está abierto un día específico.")]
        public async Task<string> ConsultarHorariosAsync(
            [Description("El nombre del negocio para buscar sus horarios")] string nombreNegocio)
        {
             var negocio = await _context.Businesses
                .Include(b => b.Schedules)
                .FirstOrDefaultAsync(b => b.TradeName.ToLower().Contains(nombreNegocio.ToLower()) && b.Status == "Approved");
                
             if (negocio == null) return "No encontré ese negocio.";
             if (negocio.Schedules == null || !negocio.Schedules.Any()) return $"El negocio {negocio.TradeName} no tiene horarios registrados.";
             
             var respuesta = $"Horarios de {negocio.TradeName}:\n";
             foreach (var schedule in negocio.Schedules)
             {
                 respuesta += $"- {schedule.DayOfWeek}: {schedule.OpenTime} a {schedule.CloseTime}\n";
             }
             return respuesta;
        }
    }
}
