using OpenCvSharp;
using OptimusFrame.Transform.Domain.Exceptions;
using OptimusFrame.Transform.Domain.Interfaces;
using OptimusFrame.Transform.Domain.ValueObjects;
using OptimusFrame.Transform.Domain.Models;

namespace OptimusFrame.Transform.Infrastructure.Services;

/// <summary>
/// Implementação do IFrameExtractionService usando OpenCvSharp
/// </summary>
public class OpenCvFrameExtractionService : IFrameExtractionService
{
    /// <summary>
    /// Extrai frames de um vídeo e salva em um diretório
    /// </summary>
    public Task<int> ExtractFramesAsync(
        string videoPath,
        string outputDirectory,
        FrameExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= FrameExtractionOptions.Optimized;

        return Task.Run(
            () => ExtractFrames(videoPath, outputDirectory, options, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Obtém informações do vídeo
    /// </summary>
    public Task<VideoInfo> GetVideoInfoAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetVideoInfo(videoPath), cancellationToken);
    }

    private int ExtractFrames(
        string videoPath,
        string outputDirectory,
        FrameExtractionOptions options,
        CancellationToken cancellationToken)
    {
        using var capture = new VideoCapture(videoPath);

        if (!capture.IsOpened())
        {
            throw new FrameExtractionException($"Não foi possível abrir o vídeo: {videoPath}");
        }

        var videoFps = capture.Fps;
        var totalFrames = capture.FrameCount;

        // Calcular intervalo entre frames baseado no TargetFps
        var frameInterval = CalculateFrameInterval(videoFps, options.TargetFps);

        var frameCount = 0;
        var currentFrame = 0;

        using var frame = new Mat();
        using var resizedFrame = new Mat();

        // Configurar parâmetros de compressão
        var (extension, encodingParams) = GetEncodingParams(options);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!capture.Read(frame) || frame.Empty())
            {
                break;
            }

            // Pular frames se necessário (baseado no TargetFps)
            if (currentFrame % frameInterval != 0)
            {
                currentFrame++;
                continue;
            }

            // Redimensionar se necessário
            var outputFrame = ResizeIfNeeded(frame, resizedFrame, options);

            // Salvar frame com compressão
            var framePath = Path.Combine(outputDirectory, $"frame_{frameCount:D6}.{extension}");
            Cv2.ImWrite(framePath, outputFrame, encodingParams);

            frameCount++;
            currentFrame++;
        }

        if (frameCount == 0)
        {
            throw new FrameExtractionException("Nenhum frame foi extraído do vídeo");
        }

        return frameCount;
    }

    private static int CalculateFrameInterval(double videoFps, double? targetFps)
    {
        if (targetFps is null or <= 0)
        {
            return 1; // Extrair todos os frames
        }

        // Se o vídeo tem 30fps e queremos 1fps, intervalo = 30
        var interval = (int)Math.Round(videoFps / targetFps.Value);
        return Math.Max(1, interval);
    }

    private static (string extension, ImageEncodingParam[] encodingParams) GetEncodingParams(
        FrameExtractionOptions options)
    {
        return options.Format switch
        {
            ImageFormat.Jpeg => ("jpg", [
                new ImageEncodingParam(ImwriteFlags.JpegQuality, options.Quality)
            ]),

            ImageFormat.WebP => ("webp", [
                new ImageEncodingParam(ImwriteFlags.WebPQuality, options.Quality)
            ]),

            ImageFormat.Png => ("png", [
                new ImageEncodingParam(ImwriteFlags.PngCompression, 9) // Máxima compressão
            ]),

            _ => ("jpg", [
                new ImageEncodingParam(ImwriteFlags.JpegQuality, options.Quality)
            ])
        };
    }

    private static Mat ResizeIfNeeded(Mat source, Mat destination, FrameExtractionOptions options)
    {
        var targetWidth = source.Width;
        var targetHeight = source.Height;
        var needsResize = false;

        // Aplicar escala
        if (options.Scale is < 1.0 and > 0)
        {
            targetWidth = (int)(source.Width * options.Scale);
            targetHeight = (int)(source.Height * options.Scale);
            needsResize = true;
        }

        // Aplicar MaxWidth/MaxHeight mantendo proporção
        if (options.MaxWidth.HasValue && targetWidth > options.MaxWidth.Value)
        {
            var ratio = (double)options.MaxWidth.Value / targetWidth;
            targetWidth = options.MaxWidth.Value;
            targetHeight = (int)(targetHeight * ratio);
            needsResize = true;
        }

        if (options.MaxHeight.HasValue && targetHeight > options.MaxHeight.Value)
        {
            var ratio = (double)options.MaxHeight.Value / targetHeight;
            targetHeight = options.MaxHeight.Value;
            targetWidth = (int)(targetWidth * ratio);
            needsResize = true;
        }

        if (!needsResize)
        {
            return source;
        }

        Cv2.Resize(source, destination, new Size(targetWidth, targetHeight), interpolation: InterpolationFlags.Area);
        return destination;
    }

    private static VideoInfo GetVideoInfo(string videoPath)
    {
        using var capture = new VideoCapture(videoPath);

        if (!capture.IsOpened())
        {
            throw new FrameExtractionException($"Não foi possível abrir o vídeo: {videoPath}");
        }

        var fps = capture.Fps;
        var frameCount = capture.FrameCount;
        var width = capture.FrameWidth;
        var height = capture.FrameHeight;

        if (fps <= 0 || frameCount <= 0)
        {
            throw new FrameExtractionException("Não foi possível obter informações válidas do vídeo");
        }

        var duration = TimeSpan.FromSeconds(frameCount / fps);

        return new VideoInfo(duration, fps, width, height);
    }
}