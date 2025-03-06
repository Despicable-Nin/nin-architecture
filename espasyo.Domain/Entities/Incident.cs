using espasyo.Domain.Common;
using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities
{

    public class Incident : BaseEntity
    {

        private double? _latitude;
        private double? _longitude;
        private long? _timestampInUnix;
        private int? _year;
        private int? _month;
        private string? _timeOfDay;

        protected Incident() { }

        public Incident(string? caseId, string? address, SeverityEnum severity, CrimeTypeEnum crimeType, MotiveEnum motive, Barangay policeDistrictEnum, 
            WeatherConditionEnum weatherCondition, string? additionalInformation, DateTimeOffset? timeStamp)
        {
            CaseId = caseId;
            Address = address;
            Severity = severity;
            CrimeType = crimeType;
            Motive = motive;
            PoliceDistrict = policeDistrictEnum;
            Weather = weatherCondition;
            AdditionalInformation = additionalInformation;
            TimeStamp = timeStamp ?? throw new ArgumentNullException(nameof(timeStamp));
            _timestampInUnix = timeStamp!.Value.ToUnixTimeMilliseconds();
            _year = timeStamp.Value.Year;
            _month = timeStamp.Value.Month;
            _timeOfDay = GetTimeOfDay(TimeStamp!.Value);
        }

        public string? CaseId { get; private set; }
        public string? Address { get; private set; }
        public string? SanitizedAddress { get; private set; }
        
        public SeverityEnum Severity { get;  set; }

        public CrimeTypeEnum CrimeType { get;  set; }

        public MotiveEnum Motive { get;  set; }

        public Barangay PoliceDistrict { get;  set; }
        public string? AdditionalInformation { get; set; }

        public WeatherConditionEnum Weather { get;  set; }

        public DateTimeOffset? TimeStamp { get; set; }

        public void ChangeLatLong(double? lat = 0, double? lng = 0)
        {
            _latitude = lat;
            _longitude = lng;
        }

        public void SanitizeAddress(string? newAddress)
        {
            SanitizedAddress = newAddress;
        }
        
        public bool IsAnomaly() => (_longitude.HasValue == false && _latitude.HasValue == false) || TimeStamp.HasValue == false;

        public double GetLatitude() => _latitude ?? 0D;
        public double GetLongitude() => _longitude ?? 0D;
        public long GetTimeStampInUnix => _timestampInUnix ?? 0L;

        public int GetYear() => _year ?? DateTimeOffset.FromUnixTimeSeconds(_timestampInUnix ?? 0L).Year;
        public int GetMonth() => _month ?? DateTimeOffset.FromUnixTimeSeconds(_timestampInUnix ?? 0L).Month;
        public string? GetTimeOfDay() => _timeOfDay;

        private string GetTimeOfDay(DateTimeOffset timeStamp)
        {
            var time = timeStamp;
            return time.Hour switch
            {
                >= 0 and < 12 => "Morning",
                >= 12 and < 18 => "Afternoon",
                _ => "Evening"
            };

        }
    }
}
