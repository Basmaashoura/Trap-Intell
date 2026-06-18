using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Shared
{
    /// <summary>
    /// Base class for aggregates that group multiple entities and value objects.
    /// </summary>
    public abstract class AggregateRoot<TId> : Abstractions.Entity<TId>
    {
        protected AggregateRoot(TId id) : base(id)
        {
        }

        protected AggregateRoot()
        {
        }
    }
}
