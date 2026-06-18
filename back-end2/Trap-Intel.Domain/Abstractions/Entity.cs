using System;
using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Base entity with generic Id type and domain events support.
    /// </summary>
    public abstract class Entity<TId> : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        protected Entity(TId id)
        {
            Id = id;
        }

        protected Entity()
        {
        }

        public TId Id { get; init; }

        public IReadOnlyList<IDomainEvent> GetDomainEvents()
        {
            return _domainEvents.ToList();
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
    }
}
