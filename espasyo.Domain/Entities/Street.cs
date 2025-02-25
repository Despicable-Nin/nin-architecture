using espasyo.Domain.Common;
using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class Street : BaseEntity
{
    public string Name { get; private set; }
    
    private int _barangay;

    protected Street()
    {
        
    }

    private Street(string name, int barangay)
    {
        Name = name;
        _barangay = barangay;
    }
    
    public Street (Barangay barangay, string? name)
    {
       _barangay = (int)barangay;
       Name = name ?? string.Empty;
    }

    public Barangay GetBarangay() => (Barangay)_barangay;


}