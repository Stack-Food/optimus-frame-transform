namespace OptimusFrame.Transform.Domain.ValueObjects;

/// <summary>
/// Value Object que representa uma chave de objeto S3
/// </summary>
public sealed record S3ObjectKey
{
    public string BucketName { get; }
    public string Key { get; }

    private S3ObjectKey(string bucketName, string key)
    {
        BucketName = bucketName;
     Key = key;
    }

    public static S3ObjectKey Create(string bucketName, string key)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        throw new ArgumentException("Bucket name cannot be empty", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        return new S3ObjectKey(bucketName, key);
    }

    public string ToUri() => $"s3://{BucketName}/{Key}";

    public override string ToString() => ToUri();
}
