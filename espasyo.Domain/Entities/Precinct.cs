using espasyo.Domain.Common;
using espasyo.Domain.Seedwork;

namespace espasyo.Domain.Entities
{
    public class Precinct : BaseEntity, IAggregateRoot
    {
        public string Name { get; set; } = string.Empty;
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

        public Precinct(string name, string code)
        {
            Name = name;
            Code = code;
            Id = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdateDetails(string name, string code, int? population = null, 
            decimal? areaKm2 = null, decimal? latitude = null, decimal? longitude = null,
            string? description = null, string? contactInfo = null)
        {
            Name = name;
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