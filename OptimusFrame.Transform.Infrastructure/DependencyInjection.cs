using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using OptimusFrame.Transform.Domain.Interfaces;
using OptimusFrame.Transform.Infrastructure.Services;
using OptimusFrame.Transform.Infrastructure.Storage;

namespace OptimusFrame.Transform.Infrastructure;

/// <summary>
/// Extensões para configuração de DI da camada Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Registrar serviços AWS
        services.AddAWSService<IAmazonS3>();

        // Registrar implementações de Domain Interfaces
        services.AddSingleton<IStorageService, S3StorageService>();
        services.AddSingleton<IFrameExtractionService, OpenCvFrameExtractionService>();
        services.AddSingleton<ICompressionService, ZipCompressionService>();

        return services;
    }
}
