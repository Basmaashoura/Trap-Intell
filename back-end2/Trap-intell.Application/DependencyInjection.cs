using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Trap_Intel.Application.Behaviors;
using Trap_Intel.Application.Billing.Services;

namespace Trap_Intel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Order is important!
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation Validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IPostPaymentSubscriptionRenewalService, PostPaymentSubscriptionRenewalService>();

        return services;
    }
}
