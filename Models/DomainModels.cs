using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinAppdePromo.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string Name { get; set; }
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string ProfilePic { get; set; }
        public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; }
        public ICollection<Business> OwnedBusinesses { get; set; }
    }

    public class UserLogin
    {
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class Business
    {
        [Key]
        public int BusinessId { get; set; }
        public int OwnerId { get; set; }
        public string RUC { get; set; }
        public string TradeName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int CategoryId { get; set; }
        public string Status { get; set; } = "Pending";
        public string ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Owner { get; set; }
        public Category Category { get; set; }
        public ICollection<BusinessImage> Images { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<BusinessSchedule> Schedules { get; set; }
        public ICollection<BusinessProduct> Products { get; set; }
    }

    public class BusinessImage
    {
        [Key]
        public int ImageId { get; set; }
        public int BusinessId { get; set; }
        public string ImageUrl { get; set; }
        public Business Business { get; set; }
    }

    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int BusinessId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Business Business { get; set; }
        public User User { get; set; }
        public ICollection<ReviewImage> Images { get; set; }
    }

    public class ReviewImage
    {
        [Key]
        public int ImageId { get; set; }
        public int ReviewId { get; set; }
        public string ImageUrl { get; set; }
        public Review Review { get; set; }
    }

    public class Favorite
    {
        public int UserId { get; set; }
        public int BusinessId { get; set; }
        public User User { get; set; }
        public Business Business { get; set; }
    }

    public class BusinessSchedule
    {
        [Key]
        public int ScheduleId { get; set; }
        public int BusinessId { get; set; }
        public string DayOfWeek { get; set; } // Ej. "Lunes-Viernes", "Sábados"
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public Business Business { get; set; }
    }

    public class BusinessProduct
    {
        [Key]
        public int ProductId { get; set; }
        public int BusinessId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsFeatured { get; set; } = false;
        public Business Business { get; set; }
    }

    public class StaffLog 
    { 
        [Key]
        public int LogId { get; set; }
        public int StaffId { get; set; }
        public string Action { get; set; }
        public int? TargetId { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public User Staff { get; set; }
    }
    
    public class BusinessReport 
    { 
        [Key]
        public int ReportId { get; set; }
        public int BusinessId { get; set; }
        public int ReporterId { get; set; }
        public string Reason { get; set; }
        public string ReportStatus { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Business Business { get; set; }
        public User Reporter { get; set; }
    }
    
    public class BusinessMetric 
    { 
        [Key]
        public int MetricId { get; set; }
        public int BusinessId { get; set; }
        public DateTime MonthYear { get; set; }
        public int Clicks { get; set; } = 0;
        public int Views { get; set; } = 0;
        public decimal AverageRating { get; set; }
        public Business Business { get; set; }
    }
}
