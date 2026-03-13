using Microsoft.Extensions.Logging;
using OptimusFrame.Transform.Application.DTOs;
using OptimusFrame.Transform.Domain.Exceptions;
using OptimusFrame.Transform.Domain.Interfaces;
using OptimusFrame.Transform.Domain.ValueObjects;
using System.Diagnostics;

namespace OptimusFrame.Transform.Application.UseCases;

/// <summary>
/// Caso de uso: Extrai frames de um vídeo e salva como ZIP no S3
/// Orquestra os Domain Services seguindo Clean Architecture
/// </summary>
public class ExtractFramesUseCase : IExtractFramesUseCase
{
    private readonly IStorageService _storageService;
    private readonly IFrameExtractionService _frameExtractionService;
    private readonly ICompressionService _compressionService;
    private readonly ILogger<ExtractFramesUseCase> _logger;

    public ExtractFramesUseCase(
        IStorageService storageService,
        IFrameExtractionService frameExtractionService,
        ICompressionService compressionService,
        ILogger<ExtractFramesUseCase> logger)
    {
        _storageService = storageService;
        _frameExtractionService = frameExtractionService;
        _compressionService = compressionService;
        _logger = logger;
    }

    public async Task<ExtractFramesResponse> ExecuteAsync(
        ExtractFramesRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempVideoPath = Path.Combine(tempDirectory, "video.mp4");
        var tempZipPath = Path.Combine(tempDirectory, "frames.zip");
        var framesDirectory = Path.Combine(tempDirectory, "frames");

        try
        {
            _logger.LogInformation(
                "Iniciando extração de frames: {BucketName}/{VideoKey}",
                request.BucketName, 
                request.VideoKey);

            // Preparar diretórios temporários
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(framesDirectory);

            // 1. Verificar se o vídeo existe e fazer download
            if (!await _storageService.ExistsAsync(request.BucketName, request.VideoKey, cancellationToken))
            {
                throw new VideoNotFoundException(request.BucketName, request.VideoKey);
            }

            _logger.LogDebug("Baixando vídeo do storage...");
            await _storageService.DownloadToFileAsync(
                request.BucketName, 
                request.VideoKey, 
                tempVideoPath, 
                cancellationToken);

            // 2. Extrair frames
            _logger.LogDebug("Extraindo frames do vídeo...");
            var extractionOptions = FrameExtractionOptions.Optimized;

            var framesExtracted = await _frameExtractionService.ExtractFramesAsync(
                tempVideoPath, 
                framesDirectory,
                extractionOptions,
                cancellationToken);

            _logger.LogInformation("{FrameCount} frames extraídos", framesExtracted);

            // 3. Comprimir frames em ZIP
            _logger.LogDebug("Comprimindo frames em ZIP...");
            await _compressionService.CompressDirectoryAsync(
                framesDirectory, 
                tempZipPath, 
                cancellationToken);

            // 4. Upload do ZIP para o storage
            _logger.LogDebug("Fazendo upload do ZIP para o storage...");
            await _storageService.UploadFromFileAsync(
                request.BucketName, 
                request.OutputZipKey, 
                tempZipPath, 
                cancellationToken);

            stopwatch.Stop();
            var outputUri = $"{request.OutputZipKey}";

            _logger.LogInformation(
                "Extração concluída com sucesso em {ElapsedTime}ms: {OutputUri}",
                stopwatch.ElapsedMilliseconds, 
                outputUri);

            return ExtractFramesResponse.Successful(outputUri, framesExtracted, stopwatch.Elapsed);
        }
        catch (VideoNotFoundException ex)
        {
            _logger.LogWarning(ex, "Vídeo não encontrado");
            return ExtractFramesResponse.Failed(ex.Message);
        }
        catch (FrameExtractionException ex)
        {
            _logger.LogError(ex, "Erro na extração de frames");
            return ExtractFramesResponse.Failed(ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operação cancelada pelo usuário");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante a extração de frames");
            return ExtractFramesResponse.Failed($"Erro inesperado: {ex.Message}");
        }
        finally
        {
            // Limpeza de arquivos temporários
            if (Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory, recursive: true);
                    _logger.LogDebug("Diretório temporário removido: {TempDir}", tempDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao remover diretório temporário: {TempDir}", tempDirectory);
                }
            }
        }
    }
}
