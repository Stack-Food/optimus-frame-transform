using OptimusFrame.Transform.Application.DTOs;

namespace OptimusFrame.Transform.Application.UseCases;

/// <summary>
/// Interface do caso de uso de extração de frames
/// Seguindo o princípio de Interface Segregation
/// </summary>
public interface IExtractFramesUseCase
{
    Task<ExtractFramesResponse> ExecuteAsync(ExtractFramesRequest request, CancellationToken cancellationToken = default);
}
