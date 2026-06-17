using Microsoft.AspNetCore.Routing;

namespace Trap_Intel.Api.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
