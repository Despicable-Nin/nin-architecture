using espasyo.Domain.Common;
using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class Manpower : BaseEntity
{
    protected Manpower() { }

    public Manpower(Guid precinctId, int year, int allocatedCount)
    {
        PrecinctId = precinctId;
        Year = year;
        AllocatedCount = allocatedCount;
        MildThreshold = 10;  // Default values
        ModerateThreshold = 25;
        CriticalThreshold = 50;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    // Legacy constructor for backward compatibility
    public Manpower(Barangay precinct, int year, int allocatedCount)
    {
        PrecinctEnum = precinct;
        Year = year;
        AllocatedCount = allocatedCount;
        MildThreshold = 10;  // Default values
        ModerateThreshold = 25;
        CriticalThreshold = 50;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Manpower(Guid precinctId, int year, int allocatedCount, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        PrecinctId = precinctId;
        Year = year;
        AllocatedCount = allocatedCount;
        MildThreshold = mildThreshold;
        ModerateThreshold = moderateThreshold;
        CriticalThreshold = criticalThreshold;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    // Legacy constructor for backward compatibility  
    public Manpower(Barangay precinct, int year, int allocatedCount, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        PrecinctEnum = precinct;
        Year = year;
        AllocatedCount = allocatedCount;
        MildThreshold = mildThreshold;
        ModerateThreshold = moderateThreshold;
        CriticalThreshold = criticalThreshold;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Foreign key to Precinct table
    public Guid PrecinctId { get; private set; }
    public virtual Precinct Precinct { get; set; } = null!;
    
    // Legacy enum property - can be removed after migration
    public Barangay PrecinctEnum { get; private set; }
    public int Year { get; private set; }
    public int AllocatedCount { get; private set; }
    public int MildThreshold { get; private set; }
    public int ModerateThreshold { get; private set; }
    public int CriticalThreshold { get; private set; }
    
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

    /// <summary>
    /// Updates the risk thresholds for this precinct
    /// </summary>
    public void UpdateThresholds(int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        if (mildThreshold < 1 || moderateThreshold < 1 || criticalThreshold < 1)
            throw new ArgumentException("All thresholds must be at least 1");
            
        if (mildThreshold >= moderateThreshold || moderateThreshold >= criticalThreshold)
            throw new ArgumentException("Thresholds must be in ascending order: mild < moderate < critical");
            
        MildThreshold = mildThreshold;
        ModerateThreshold = moderateThreshold;
        CriticalThreshold = criticalThreshold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates both allocation and thresholds in a single operation
    /// </summary>
    public void UpdateAllocationAndThresholds(int newCount, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        UpdateAllocation(newCount);
        UpdateThresholds(mildThreshold, moderateThreshold, criticalThreshold);
    }

    /// <summary>
    /// Gets the severity level based on predicted case count
    /// </summary>
    public string GetSeverityLevel(int predictedCases)
    {
        if (predictedCases <= MildThreshold)
            return "Mild";
        if (predictedCases <= ModerateThreshold)
            return "Moderate";
        if (predictedCases <= CriticalThreshold)
            return "High";
        return "Critical";
    }

    /// <summary>
    /// Determines if manpower adjustment is needed based on predicted cases
    /// </summary>
    public bool RequiresManpowerAdjustment(int predictedCases)
    {
        var adjustment = GetRecommendedManpowerAdjustment(predictedCases);
        return adjustment != 0;
    }

    /// <summary>
    /// Calculates recommended manpower adjustment based on predicted cases
    /// </summary>
    public int GetRecommendedManpowerAdjustment(int predictedCases)
    {
        var casesPerOfficer = AllocatedCount > 0 ? (double)predictedCases / AllocatedCount : predictedCases;
        
        // If cases per officer is too high, we need more officers
        if (predictedCases > CriticalThreshold)
        {
            // For critical cases, aim for max 8 cases per officer
            var idealOfficers = Math.Ceiling(predictedCases / 8.0);
            return Math.Max(0, (int)idealOfficers - AllocatedCount);
        }
        
        if (predictedCases > ModerateThreshold)
        {
            // For moderate cases, aim for max 12 cases per officer  
            var idealOfficers = Math.Ceiling(predictedCases / 12.0);
            return Math.Max(0, (int)idealOfficers - AllocatedCount);
        }
        
        // For mild cases, we might be able to reduce officers
        if (predictedCases <= MildThreshold && AllocatedCount > 2)
        {
            // Aim for at least 5 cases per officer, but keep minimum of 2 officers
            var idealOfficers = Math.Max(2, Math.Ceiling(predictedCases / 5.0));
            return (int)idealOfficers - AllocatedCount;
        }
        
        return 0; // No adjustment needed
    }
}
