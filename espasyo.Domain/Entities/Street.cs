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


    public Street (Barangay barangay, string? name)
    {
       _barangay = (int)barangay;
       Name = name ?? string.Empty;
    }

    public Barangay GetBarangay() => (Barangay)_barangay;


}