using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class Manpower
{
    protected Manpower() { }

    public Manpower(Guid precinctId, ShiftEnum shift, int headCount)
    {
        Id = Guid.NewGuid();
        PrecinctId = precinctId;
        Shift = shift;
        HeadCount = headCount;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid PrecinctId { get; private set; }
    public ShiftEnum Shift { get; private set; }
    public virtual Precinct Precinct { get; set; } = null!;
    public int HeadCount { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }

    /// <summary>
    /// Updates the head count for this precinct and shift
    /// </summary>
    public void UpdateHeadCount(int newHeadCount)
    {
        if (newHeadCount < 0)
            throw new ArgumentException("Head count cannot be negative");
            
        HeadCount = newHeadCount;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Calculates shortage or overage based on required manpower
    /// </summary>
    /// <param name="requiredCount">The required number of personnel</param>
    /// <returns>Positive number indicates overage, negative indicates shortage</returns>
    public int CalculateVariance(int requiredCount)
    {
        return HeadCount - requiredCount;
    }

    /// <summary>
    /// Determines if there's a shortage of manpower
    /// </summary>
    public bool HasShortage(int requiredCount)
    {
        return HeadCount < requiredCount;
    }

    /// <summary>
    /// Determines if there's an overage of manpower
    /// </summary>
    public bool HasOverage(int requiredCount)
    {
        return HeadCount > requiredCount;
    }

    /// <summary>
    /// Gets the display name for the shift
    /// </summary>
    public string GetShiftDisplayName()
    {
        return Shift switch
        {
            ShiftEnum.Morning => "Morning (6:00 AM - 2:00 PM)",
            ShiftEnum.Evening => "Evening (2:00 PM - 10:00 PM)",
            ShiftEnum.Night => "Night (10:00 PM - 6:00 AM)",
            _ => Shift.ToString()
        };
    }

    /// <summary>
    /// Determines if this manpower allocation is for a specific shift
    /// </summary>
    public bool IsForShift(ShiftEnum shift)
    {
        return Shift == shift;
    }
}
