using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities
{

    public class Incident
    {

        private float? _latitude;
        private float? _longitude;

        protected Incident() { }

        public Incident(string? caseId, string? address, SeverityEnum severity, CrimeTypeEnum crimeType, MotiveEnum motive, MuntinlupaPoliceDistrictEnum policeDistrictEnum, 
            WeatherConditionEnum weatherCondition, string? otherMotive, DateTimeOffset? timeStamp)
        {
            CaseId = caseId;
            Address = address;
            Severity = severity;
            CrimeType = crimeType;
            Motive = motive;
            PoliceDistrict = policeDistrictEnum;
            Weather = weatherCondition;
            OtherMotive = otherMotive;
            TimeStamp = timeStamp;
        }

        public Guid Id { get; protected set; }
        public string? CaseId { get; private set; }
        public string? Address { get; private set; }
        
        public SeverityEnum Severity { get;  set; }

        public CrimeTypeEnum CrimeType { get;  set; }

        public MotiveEnum Motive { get;  set; }

        public MuntinlupaPoliceDistrictEnum PoliceDistrict { get;  set; }
        public string? OtherMotive { get; set; }

        public WeatherConditionEnum Weather { get;  set; }

        public DateTimeOffset? TimeStamp { get; set; }


        public void ChangeLatLong(float? lat = 0, float? lng = 0)
        {
            _latitude = lat;
            _longitude = lng;
        }

        public float? GetLatitude() => _latitude;
        public float? GetLongitude() => _longitude;

    }
}
