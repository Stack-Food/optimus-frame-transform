using OptimusFrame.Transform.Application.DTOs;
using OptimusFrame.Transform.Application.UseCases;

namespace OptimusFrame.Transform.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var extractFramesUseCase = scope.ServiceProvider
            .GetRequiredService<IExtractFramesUseCase>();

        // TODO: Substituir por leitura de fila (SQS, RabbitMQ, etc.)
        var request = new ExtractFramesRequest(
            BucketName: "teste-bucket-optimus",
            VideoKey: "input/video.mp4",
            OutputZipKey: "output/frames.zip"
        );

        try
        {
            _logger.LogInformation("Iniciando processamento do vídeo: {VideoKey}", request.VideoKey);

            var response = await extractFramesUseCase.ExecuteAsync(request, stoppingToken);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Processamento concluído com sucesso! {FrameCount} frames extraídos em {Time:F2}s. Output: {OutputUri}",
                    response.FramesExtracted,
                    response.ProcessingTime.TotalSeconds,
                    response.OutputUri);
            }
            else
            {
                _logger.LogError("Falha no processamento: {ErrorMessage}", response.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processamento cancelado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar vídeo.");
        }
    }
}
