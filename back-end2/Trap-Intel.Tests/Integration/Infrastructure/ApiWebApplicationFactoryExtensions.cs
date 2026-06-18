using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Trap_Intel.Tests.Integration.Infrastructure;

internal static class ApiWebApplicationFactoryExtensions
{
    public static HttpClient CreateClientWithSender(this ApiWebApplicationFactory factory, ISender sender)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISender>();
                services.AddSingleton(sender);
            });
        }).CreateClient();
    }
}
