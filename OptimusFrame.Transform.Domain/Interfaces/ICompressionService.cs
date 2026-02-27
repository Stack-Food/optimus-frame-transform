namespace OptimusFrame.Transform.Domain.Interfaces;

/// <summary>
/// Interface para compressão de arquivos (Port)
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Comprime um diretório em um arquivo ZIP
    /// </summary>
    /// <param name="sourceDirectory">Diretório fonte</param>
    /// <param name="destinationZipPath">Caminho do arquivo ZIP de destino</param>
    Task CompressDirectoryAsync(
        string sourceDirectory,
        string destinationZipPath,
        CancellationToken cancellationToken = default);
}
