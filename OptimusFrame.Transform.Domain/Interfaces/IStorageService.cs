namespace OptimusFrame.Transform.Domain.Interfaces;

/// <summary>
/// Interface para serviço de armazenamento de objetos (Port)
/// Seguindo o princípio de inversão de dependência - Domain não conhece S3
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Faz download de um arquivo para um caminho local
    /// </summary>
    Task DownloadToFileAsync(string bucketName, string key, string localPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Faz upload de um arquivo local para o storage
    /// </summary>
    Task UploadFromFileAsync(string bucketName, string key, string localPath, CancellationToken cancellationToken = default);

    /// <summary>
  /// Verifica se um objeto existe no storage
    /// </summary>
    Task<bool> ExistsAsync(string bucketName, string key, CancellationToken cancellationToken = default);
}
