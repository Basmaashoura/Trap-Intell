using System.Collections.Generic;

namespace Trap_Intel.Domain.Abstractions
{
    public interface IHasDomainEvents
    {
        IReadOnlyList<IDomainEvent> GetDomainEvents();
        void ClearDomainEvents();
    }
}