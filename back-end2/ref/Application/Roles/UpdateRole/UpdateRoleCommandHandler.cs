using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.UpdateRole;

/// <summary>
/// Handler for UpdateRoleCommand.
/// Updates an existing role.
/// </summary>
internal sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.RoleId));
        }

        // Check if new name conflicts with existing role (case-insensitive, excluding current role)
#pragma warning disable CA1304, CA1311, CA1862 // Culture warnings - executed in database
        bool nameExists = await _context.Roles
            .AnyAsync(r => r.Name.ToUpper() == command.Name.ToUpper() && r.Id != command.RoleId, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862

        if (nameExists)
        {
            return Result.Failure(RoleErrors.NameNotUnique);
        }

        // Update role (Domain method handles system role check and raises event)
        Result updateResult = role.Update(command.Name, command.Description, command.CanViewSensitiveData);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
