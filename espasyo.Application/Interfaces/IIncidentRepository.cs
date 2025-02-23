﻿using espasyo.Domain.Entities;

namespace espasyo.Application.Common.Interfaces;

public interface IIncidentRepository
{
    Task<IEnumerable<Incident>> GetAllIncidentsAsync(KeyValuePair<DateOnly, DateOnly>? dateRange = null);
    Task<(IEnumerable<Incident>,int count)> GetPaginatedIncidentsAsync(int pageNumber, int pageSize);
    Task<Incident?> GetIncidentByCaseIdAsync(string caseId);
    Task<Incident?> GetIncidentByIdAsync(Guid id);
    Task<Incident> CreateIncidentAsync(Incident incident);
    Task<Incident?> UpdateIncidentAsync(Incident incident);
    Dictionary<int, string> GetCrimeTypes();
    Dictionary<int, string> GetSeverityEnums();
    Dictionary<int, string> GetMotiveEnums();
    Dictionary<int, string> PoliceDistrictEnums();
    Dictionary<int, string> GetWeatherEnums();
}