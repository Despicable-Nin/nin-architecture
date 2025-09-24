using espasyo.Domain.Common;
using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class Manpower : BaseEntity
{
    protected Manpower() { }

    public Manpower(Barangay precinct, int year, int allocatedCount, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        Precinct = precinct;
        Year = year;
        AllocatedCount = allocatedCount;
        MildThreshold = mildThreshold;
        ModerateThreshold = moderateThreshold;
        CriticalThreshold = criticalThreshold;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Barangay Precinct { get; private set; }
    public int Year { get; private set; }
    public int AllocatedCount { get; private set; }
    
    // Thresholds for determining severity levels
    public int MildThreshold { get; private set; }       // Cases <= this number = Mild
    public int ModerateThreshold { get; private set; }   // Cases <= this number = Moderate
    public int CriticalThreshold { get; private set; }   // Cases > this number = Critical
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateAllocation(int newCount)
    {
        AllocatedCount = newCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateThresholds(int mild, int moderate, int critical)
    {
        if (mild >= moderate || moderate >= critical)
        {
            throw new ArgumentException("Thresholds must be in ascending order: mild < moderate < critical");
        }
        
        MildThreshold = mild;
        ModerateThreshold = moderate;
        CriticalThreshold = critical;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string GetSeverityLevel(int caseCount)
    {
        if (caseCount <= MildThreshold)
            return "Mild";
        if (caseCount <= ModerateThreshold)
            return "Moderate";
        return "Critical";
    }

    public bool RequiresManpowerAdjustment(int predictedCaseCount)
    {
        // Use scientific thresholds based on workload analysis
        var severity = GetSeverityLevel(predictedCaseCount);
        
        return severity switch
        {
            "Critical" => true, // Always review critical levels
            "Mild" => predictedCaseCount < MildThreshold * 0.75, // 25% buffer for mild cases
            _ => false // Moderate levels are generally acceptable
        };
    }

    public int GetRecommendedManpowerAdjustment(int predictedCaseCount)
    {
        if (!RequiresManpowerAdjustment(predictedCaseCount))
            return 0;

        var severity = GetSeverityLevel(predictedCaseCount);
        
        return severity switch
        {
            "Critical" => CalculateCriticalLevelAdjustment(predictedCaseCount),
            "Mild" => CalculateMildLevelReduction(predictedCaseCount),
            _ => 0
        };
    }

    private int CalculateCriticalLevelAdjustment(int predictedCaseCount)
    {
        // Calculate additional officers needed based on workload capacity
        const double CASES_PER_OFFICER_PER_MONTH = 15.0; // Conservative estimate for mixed case types
        
        var excessCases = predictedCaseCount - CriticalThreshold;
        var additionalOfficersNeeded = (int)Math.Ceiling(excessCases / CASES_PER_OFFICER_PER_MONTH);
        
        // Cap increase at 50% of current allocation to prevent unrealistic jumps
        return Math.Min(additionalOfficersNeeded, AllocatedCount / 2);
    }

    private int CalculateMildLevelReduction(int predictedCaseCount)
    {
        // Calculate potential reduction based on underutilization
        const double MINIMUM_UTILIZATION = 0.6; // 60% minimum utilization
        
        var currentCapacity = AllocatedCount * 15.0; // Cases per officer capacity
        var utilizationRate = predictedCaseCount / currentCapacity;
        
        if (utilizationRate < MINIMUM_UTILIZATION)
        {
            var excessCapacity = currentCapacity - (predictedCaseCount / MINIMUM_UTILIZATION);
            var potentialReduction = (int)Math.Floor(excessCapacity / 15.0);
            
            // Never reduce below minimum staffing requirements (2 officers minimum)
            return -Math.Min(potentialReduction, AllocatedCount - 2);
        }
        
        return 0;
    }
}