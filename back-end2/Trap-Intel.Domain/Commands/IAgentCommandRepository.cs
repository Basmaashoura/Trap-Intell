using Trap_Intel.Domain.Commands.Enums;

namespace Trap_Intel.Domain.Commands;

public interface IAgentCommandRepository
{
    Task<AgentCommand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetByHoneypotIdAsync(Guid honeypotId, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetPendingCommandsAsync(Guid honeypotId, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetByStatusAsync(AgentCommandStatus status, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetTimedOutCommandsAsync(DateTime before, CancellationToken cancellationToken = default);
    Task<List<AgentCommand>> GetFailedCommandsAsync(Guid honeypotId, CancellationToken cancellationToken = default);
    Task AddAsync(AgentCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentCommand command, CancellationToken cancellationToken = default);
    Task<int> CountByHoneypotAsync(Guid honeypotId, CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(Guid honeypotId, CancellationToken cancellationToken = default);
}
