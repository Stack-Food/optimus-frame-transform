using Amazon.S3;
using Amazon.S3.Model;
using OptimusFrame.Transform.Domain.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace OptimusFrame.Transform.Infrastructure.Storage;

[ExcludeFromCodeCoverage]
/// <summary>
/// Implementação do IStorageService usando Amazon S3
/// </summary>
public class S3StorageService : IStorageService
{
 private readonly IAmazonS3 _s3Client;

    public S3StorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task DownloadToFileAsync(
        string bucketName,
        string key,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.GetObjectAsync(bucketName, key, cancellationToken);

        await using var fileStream = File.Create(localPath);
        await response.ResponseStream.CopyToAsync(fileStream, cancellationToken);
    }

    public async Task UploadFromFileAsync(
        string bucketName,
        string key,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            FilePath = localPath
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string bucketName,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(bucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            Console.WriteLine($"S3 Error: {ex.Message}");
            Console.WriteLine($"Code: {ex.ErrorCode}");
            Console.WriteLine($"Status: {ex.StatusCode}");

            throw;
        }
    }
}
