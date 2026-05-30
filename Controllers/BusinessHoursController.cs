using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using System.Threading.Tasks;

namespace PinAppdePromo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessHoursController : ControllerBase
    {
        private readonly IBusinessHoursService _hoursService;
        private readonly PinDbContext _context;

        public BusinessHoursController(IBusinessHoursService hoursService, PinDbContext context)
        {
            _hoursService = hoursService;
            _context = context;
        }

        /// <summary>
        /// Obtiene el estado actual de un negocio (abierto/cerrado)
        /// GET /api/businesshours/status/5
        /// </summary>
        [HttpGet("status/{businessId}")]
        public async Task<IActionResult> GetStatus(int businessId)
        {
            var status = await _hoursService.GetCurrentStatusAsync(businessId);
            return Ok(status);
        }

        /// <summary>
        /// Obtiene el horario de toda la semana
        /// GET /api/businesshours/week/5
        /// </summary>
        [HttpGet("week/{businessId}")]
        public async Task<IActionResult> GetWeekSchedule(int businessId)
        {
            var schedules = await _hoursService.GetWeekScheduleAsync(businessId);
            return Ok(schedules);
        }

        /// <summary>
        /// Guarda o actualiza los horarios de un negocio
        /// POST /api/businesshours/save
        /// Body: { businessId, schedules: [ { dayOfWeek, openTime, closeTime }, ... ] }
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveSchedules([FromBody] SaveScheduleRequest request)
        {
            if (request?.Schedules == null || request.Schedules.Count == 0)
                return BadRequest("No schedules provided");

            try
            {
                // Eliminar horarios antiguos
                var oldSchedules = await _context.BusinessSchedules
                    .Where(s => s.BusinessId == request.BusinessId)
                    .ToListAsync();
                _context.BusinessSchedules.RemoveRange(oldSchedules);

                // Agregar nuevos horarios
                foreach (var schedule in request.Schedules)
                {
                    var newSchedule = new BusinessSchedule
                    {
                        BusinessId = request.BusinessId,
                        DayOfWeek = schedule.DayOfWeek,
                        OpenTime = TimeSpan.Parse(schedule.OpenTime),
                        CloseTime = TimeSpan.Parse(schedule.CloseTime)
                    };
                    _context.BusinessSchedules.Add(newSchedule);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Horarios guardados exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SaveScheduleRequest
    {
        public int BusinessId { get; set; }
        public List<ScheduleInput> Schedules { get; set; }
    }

    public class ScheduleInput
    {
        public string DayOfWeek { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
    }
}
