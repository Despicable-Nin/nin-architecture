﻿using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    public DbSet<Incident> Incidents { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}