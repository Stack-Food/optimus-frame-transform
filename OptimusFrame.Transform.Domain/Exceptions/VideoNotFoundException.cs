namespace OptimusFrame.Transform.Domain.Exceptions;

public class VideoNotFoundException : DomainException
{
    public string BucketName { get; }
    public string VideoKey { get; }

    public VideoNotFoundException(string bucketName, string videoKey)
        : base($"Vídeo não encontrado: s3://{bucketName}/{videoKey}")
    {
        BucketName = bucketName;
        VideoKey = videoKey;
    }
}
