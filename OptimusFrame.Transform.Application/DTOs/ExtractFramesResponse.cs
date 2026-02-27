namespace OptimusFrame.Transform.Application.DTOs;

/// <summary>
/// DTO de resposta da extração de frames
/// </summary>
public record ExtractFramesResponse(
    bool Success,
    string OutputUri,
    int FramesExtracted,
    TimeSpan ProcessingTime,
    string? ErrorMessage = null
)
{
    public static ExtractFramesResponse Successful(
        string outputUri,
        int framesExtracted,
        TimeSpan processingTime)
        => new(true, outputUri, framesExtracted, processingTime);

    public static ExtractFramesResponse Failed(string errorMessage)
        => new(false, string.Empty, 0, TimeSpan.Zero, errorMessage);
}
