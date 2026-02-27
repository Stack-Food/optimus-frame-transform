namespace OptimusFrame.Transform.Application.DTOs;

/// <summary>
/// DTO de entrada para extração de frames
/// </summary>
public record ExtractFramesRequest(
    string BucketName,
    string VideoKey,
    string OutputZipKey
);
