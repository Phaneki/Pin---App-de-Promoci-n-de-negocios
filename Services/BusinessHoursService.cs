using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PinAppdePromo.Models;
using Microsoft.EntityFrameworkCore;

namespace PinAppdePromo.Services
{
    public interface IBusinessHoursService
    {
        Task<BusinessHoursStatus> GetCurrentStatusAsync(int businessId, DateTime? dateTime = null);
        Task<IEnumerable<BusinessSchedule>> GetWeekScheduleAsync(int businessId);
        bool IsOpenNow(int businessId, List<BusinessSchedule> schedules, DateTime? dateTime = null);
        string GetDayOfWeek(DateTime dateTime);
    }

    public class BusinessHoursService : IBusinessHoursService
    {
        private readonly PinDbContext _context;

        public BusinessHoursService(PinDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el estado actual de un negocio (abierto, cerrado, horarios)
        /// </summary>
        public async Task<BusinessHoursStatus> GetCurrentStatusAsync(int businessId, DateTime? dateTime = null)
        {
            dateTime ??= DateTime.Now;
            var schedules = await GetWeekScheduleAsync(businessId);

            var status = new BusinessHoursStatus
            {
                BusinessId = businessId,
                CurrentTime = dateTime.Value,
                DayOfWeek = GetDayOfWeek(dateTime.Value),
                IsOpen = IsOpenNow(businessId, schedules.ToList(), dateTime),
                Schedules = schedules.ToList()
            };

            // Obtener horarios del día actual
            var todaySchedules = schedules.Where(s => s.DayOfWeek == status.DayOfWeek).ToList();
            
            if (todaySchedules.Any())
            {
                var currentTime = dateTime.Value.TimeOfDay;
                var todaySchedule = todaySchedules.First();

                status.OpenTime = todaySchedule.OpenTime;
                status.CloseTime = todaySchedule.CloseTime;

                if (status.IsOpen)
                {
                    // Calcula cuántos minutos faltan para cerrar
                    var timeUntilClose = todaySchedule.CloseTime - currentTime;
                    status.MinutesUntilClose = (int)timeUntilClose.TotalMinutes;
                    status.ClosesAt = dateTime.Value.Date.Add(todaySchedule.CloseTime);
                }
                else
                {
                    // Calcula cuándo abre
                    var tomorrowSchedules = schedules.Where(s => s.DayOfWeek == GetDayOfWeek(dateTime.Value.AddDays(1))).ToList();
                    if (tomorrowSchedules.Any())
                    {
                        status.OpensAt = dateTime.Value.AddDays(1).Date.Add(tomorrowSchedules.First().OpenTime);
                    }
                }
            }
            else
            {
                status.IsOpen = false;
                status.OpensAt = null;
            }

            return status;
        }

        /// <summary>
        /// Obtiene el horario de toda la semana para un negocio
        /// </summary>
        public async Task<IEnumerable<BusinessSchedule>> GetWeekScheduleAsync(int businessId)
        {
            try
            {
                return await _context.BusinessSchedules
                    .Where(s => s.BusinessId == businessId)
                    .OrderBy(s => s.DayOfWeek)
                    .ToListAsync();
            }
            catch (Exception ex) when (ex.Message.Contains("BusinessSchedules"))
            {
                // La tabla no existe aún, retornar lista vacía
                return new List<BusinessSchedule>();
            }
        }

        /// <summary>
        /// Determina si un negocio está abierto en el momento especificado
        /// </summary>
        public bool IsOpenNow(int businessId, List<BusinessSchedule> schedules, DateTime? dateTime = null)
        {
            dateTime ??= DateTime.Now;
            var currentTime = dateTime.Value.TimeOfDay;
            var dayOfWeek = GetDayOfWeek(dateTime.Value);

            var todaySchedules = schedules.Where(s => s.DayOfWeek == dayOfWeek).ToList();

            if (!todaySchedules.Any())
                return false;

            // Soporta múltiples turnos en el mismo día
            return todaySchedules.Any(s => currentTime >= s.OpenTime && currentTime < s.CloseTime);
        }

        /// <summary>
        /// Convierte DateTime a nombre del día en español
        /// </summary>
        public string GetDayOfWeek(DateTime dateTime)
        {
            return dateTime.DayOfWeek switch
            {
                System.DayOfWeek.Monday => "Monday",
                System.DayOfWeek.Tuesday => "Tuesday",
                System.DayOfWeek.Wednesday => "Wednesday",
                System.DayOfWeek.Thursday => "Thursday",
                System.DayOfWeek.Friday => "Friday",
                System.DayOfWeek.Saturday => "Saturday",
                System.DayOfWeek.Sunday => "Sunday",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// DTO para responder con el estado actual de un negocio
    /// </summary>
    public class BusinessHoursStatus
    {
        public int BusinessId { get; set; }
        public DateTime CurrentTime { get; set; }
        public string DayOfWeek { get; set; }
        public bool IsOpen { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int MinutesUntilClose { get; set; } // Si está abierto
        public DateTime? ClosesAt { get; set; }
        public DateTime? OpensAt { get; set; } // Si está cerrado
        public List<BusinessSchedule> Schedules { get; set; } = new();

        public string GetStatusBadge()
        {
            if (IsOpen)
            {
                if (MinutesUntilClose <= 30)
                    return $"⚠️ Cierra en {MinutesUntilClose} minutos";
                return $"✅ Abierto • Cierra a las {CloseTime?.ToString("HH:mm")}";
            }
            return OpensAt.HasValue ? $"❌ Cerrado • Abre {OpensAt:HH:mm}" : "❌ Horario no disponible";
        }
    }
}
