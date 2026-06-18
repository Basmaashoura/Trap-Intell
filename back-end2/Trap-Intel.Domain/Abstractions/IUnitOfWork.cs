using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Unit of Work abstraction to coordinate repositories and commit transactions.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
