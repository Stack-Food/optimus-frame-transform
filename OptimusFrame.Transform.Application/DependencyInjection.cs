using Microsoft.Extensions.DependencyInjection;
using OptimusFrame.Transform.Application.UseCases;

namespace OptimusFrame.Transform.Application;

/// <summary>
/// Extensões para configuração de DI da camada Application
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registrar Use Cases
        services.AddScoped<IExtractFramesUseCase, ExtractFramesUseCase>();

        return services;
    }
}
