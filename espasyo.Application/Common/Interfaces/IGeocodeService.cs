namespace espasyo.Application.Common.Interfaces;

public interface IGeocodeService
{
    Task<(double Latitude, double Longitude)> GetLatLongAsync(string address);
}