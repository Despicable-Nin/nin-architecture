using espasyo.Domain.Common;
using espasyo.Domain.Enums;
using espasyo.Domain.Seedwork;

namespace espasyo.Domain.Entities
{
    public class Precinct : BaseEntity, IAggregateRoot
    {
        public Barangay Barangay { get; set; } = Barangay.Alabang;
        
        public string Name => Barangay.ToString().Replace("_", " "); // Convert enum to readable name
        public string Code { get; set; } = string.Empty;
        public int? Population { get; set; }
        public decimal? AreaKm2 { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public string? ContactInfo { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Incident> Incidents { get; set; } = new List<Incident>();
        public virtual ICollection<Manpower> ManpowerAllocations { get; set; } = new List<Manpower>();
        public virtual ICollection<Street> Streets { get; set; } = new List<Street>();

        public Precinct()
        {
        }

    public Precinct(Barangay barangay, string code)
    {
        Barangay = barangay;
        Code = code;
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(Barangay barangay, string code, int? population = null, 
        decimal? areaKm2 = null, decimal? latitude = null, decimal? longitude = null,
        string? description = null, string? contactInfo = null)
    {
        Barangay = barangay;
        Code = code;
        Population = population;
        AreaKm2 = areaKm2;
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        ContactInfo = contactInfo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}