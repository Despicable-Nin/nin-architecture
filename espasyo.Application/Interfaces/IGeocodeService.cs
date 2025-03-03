﻿namespace espasyo.Application.Interfaces;

public interface IGeocodeService
{
    Task<(double? Latitude, double? Longitude, string NewAddress)> GetLatLongAsync(string address);
}