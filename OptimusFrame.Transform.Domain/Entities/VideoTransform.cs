using OptimusFrame.Transform.Domain.Enums;

namespace OptimusFrame.Transform.Domain.Entities;

/// <summary>
/// Entidade que representa uma requisição de transformação de vídeo
/// </summary>
public class VideoTransform
{
    public Guid Id { get; private set; }
    public string BucketName { get; private set; }
    public string VideoKey { get; private set; }
    public string OutputZipKey { get; private set; }
    public VideoTransformStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private VideoTransform() { }

    public static VideoTransform Create(string bucketName, string videoKey, string outputZipKey)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name é obrigatório", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(videoKey))
            throw new ArgumentException("Video key é obrigatória", nameof(videoKey));

        if (string.IsNullOrWhiteSpace(outputZipKey))
            throw new ArgumentException("Output zip key é obrigatória", nameof(outputZipKey));

        return new VideoTransform
        {
            Id = Guid.NewGuid(),
            BucketName = bucketName,
            VideoKey = videoKey,
            OutputZipKey = outputZipKey,
            Status = VideoTransformStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessing()
    {
        if (Status != VideoTransformStatus.Pending)
            throw new InvalidOperationException($"Não é possível processar uma requisição com status {Status}");

        Status = VideoTransformStatus.Processing;
    }

    public void MarkAsCompleted()
    {
        if (Status != VideoTransformStatus.Processing)
          throw new InvalidOperationException($"Não é possível completar uma requisição com status {Status}");

        Status = VideoTransformStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = VideoTransformStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
