using System.IO.Compression;
using OptimusFrame.Transform.Domain.Interfaces;

namespace OptimusFrame.Transform.Infrastructure.Services;

/// <summary>
/// Implementação do ICompressionService usando System.IO.Compression
/// </summary>
public class ZipCompressionService : ICompressionService
{
    public Task CompressDirectoryAsync(
        string sourceDirectory,
        string destinationZipPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ZipFile.CreateFromDirectory(
            sourceDirectory,
            destinationZipPath,
            CompressionLevel.Optimal,
            includeBaseDirectory: false
        );

        return Task.CompletedTask;
    }
}
