using OptimusFrame.Transform.Domain.Models;
using OptimusFrame.Transform.Domain.ValueObjects;

namespace OptimusFrame.Transform.Domain.Interfaces;

/// <summary>
/// Interface para extração de frames de vídeo (Port)
/// Domain Service Interface - operação de domínio que não pertence a uma entidade específica
/// </summary>
public interface IFrameExtractionService
{
    /// <summary>
    /// Extrai frames de um vídeo e salva em um diretório
    /// </summary>
    /// <param name="videoPath">Caminho local do vídeo</param>
    /// <param name="outputDirectory">Diretório onde os frames serão salvos</param>
    /// <param name="options">Opções de extração</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Número de frames extraídos</returns>
    Task<int> ExtractFramesAsync(
        string videoPath,
        string outputDirectory,
        FrameExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém informações do vídeo
    /// </summary>
    Task<VideoInfo> GetVideoInfoAsync(
        string videoPath,
        CancellationToken cancellationToken = default);
}
