using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.CreateRole;

/// <summary>
/// Handler for CreateRoleCommand.
/// Creates a new role in the system.
/// </summary>
internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        // Check if role name already exists (case-insensitive using EF.Functions)
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
        bool nameExists = await _context.Roles
            .AnyAsync(r => r.Name.ToUpper() == command.Name.ToUpper(), cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        if (nameExists)
        {
            return Result.Failure<Guid>(RoleErrors.NameNotUnique);
        }

        // Create new role (Domain method raises RoleCreatedDomainEvent)
        var role = Role.Create(
            command.Name,
            command.Description,
            command.CanViewSensitiveData,
            isSystemRole: false);

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(role.Id);
    }
}
