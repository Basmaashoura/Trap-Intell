using System;
using MediatR;

namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Marker interface for domain events.
    /// Extends INotification so that MediatR can publish them.
    /// </summary>
    public interface IDomainEvent : INotification
    {
        DateTime OccurredOn { get; }
    }
}
