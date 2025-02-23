﻿using MediatR;
using System.ComponentModel.DataAnnotations.Schema;

namespace espasyo.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }
        private readonly List<BaseEvent> _domainEvents = [];

        [NotMapped]
        public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(BaseEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(BaseEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    public abstract class BaseEvent : INotification { }
}
