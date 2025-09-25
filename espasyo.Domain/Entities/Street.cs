using espasyo.Domain.Common;

namespace espasyo.Domain.Entities;

public class Street : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid PrecinctId { get; private set; }
    public virtual Precinct Precinct { get; set; } = null!;

    protected Street()
    {
        
    }

    public Street(Guid precinctId, string name)
    {
        PrecinctId = precinctId;
        Name = name ?? string.Empty;
        Id = Guid.NewGuid();
    }

    public void UpdateName(string newName)
    {
        Name = newName ?? string.Empty;
    }
}
