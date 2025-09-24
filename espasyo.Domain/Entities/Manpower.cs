using espasyo.Domain.Common;
using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class Manpower : BaseEntity
{
    protected Manpower() { }

    public Manpower(Barangay precinct, int year, int allocatedCount)
    {
        Precinct = precinct;
        Year = year;
        AllocatedCount = allocatedCount;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Barangay Precinct { get; private set; }
    public int Year { get; private set; }
    public int AllocatedCount { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Updates the allocated manpower count for this precinct/year
    /// </summary>
    public void UpdateAllocation(int newCount)
    {
        if (newCount < 1)
            throw new ArgumentException("Allocated count must be at least 1");
            
        AllocatedCount = newCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}