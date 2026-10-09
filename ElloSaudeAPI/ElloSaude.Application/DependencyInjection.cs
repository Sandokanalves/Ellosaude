using System.Reflection;
using ElloSaude.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ElloSaude.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(assembly);
        });

        // FluentValidation - Registra todos os validadores do assembly de Application
        services.AddValidatorsFromAssembly(assembly);

        // MediatR com Pipeline Behavior de validação
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
