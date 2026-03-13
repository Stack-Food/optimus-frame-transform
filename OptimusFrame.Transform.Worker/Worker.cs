using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OptimusFrame.Transform.Application.DTOs;
using OptimusFrame.Transform.Application.UseCases;
using OptimusFrame.Transform.Worker.Configuration;
using OptimusFrame.Transform.Worker.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OptimusFrame.Transform.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly StorageSettings _storageSettings;
    private IConnection? _connection;
    private IModel? _channel;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> rabbitMqSettings,
        IOptions<StorageSettings> storageSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _rabbitMqSettings = rabbitMqSettings.Value;
        _storageSettings = storageSettings.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        InitializeRabbitMq();
        return base.StartAsync(cancellationToken);
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqSettings.HostName,
                Port = _rabbitMqSettings.Port,
                UserName = _rabbitMqSettings.UserName,
                Password = _rabbitMqSettings.Password,
                VirtualHost = _rabbitMqSettings.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declara fila de entrada
            _channel.QueueDeclare(
                queue: _rabbitMqSettings.InputQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Declara fila de conclusão
            _channel.QueueDeclare(
                queue: _rabbitMqSettings.CompletedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: _rabbitMqSettings.PrefetchCount,
                global: false);

            _logger.LogInformation(
                "Conectado ao RabbitMQ em {Host}:{Port}, ouvindo fila '{InputQueue}', publicando em '{CompletedQueue}'",
                _rabbitMqSettings.HostName,
                _rabbitMqSettings.Port,
                _rabbitMqSettings.InputQueueName,
                _rabbitMqSettings.CompletedQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
            throw;
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            _logger.LogError("Canal RabbitMQ não inicializado");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            await ProcessMessageAsync(eventArgs, stoppingToken);
        };

        _channel.BasicConsume(
            queue: _rabbitMqSettings.InputQueueName,
            autoAck: _rabbitMqSettings.AutoAck,
            consumer: consumer);

        _logger.LogInformation("Worker iniciado e aguardando mensagens...");

        // Mantém o worker rodando até o cancelamento
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        VideoProcessingMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<VideoProcessingMessage>(messageBody);

            if (message is null || string.IsNullOrWhiteSpace(message.VideoId))
            {
                _logger.LogWarning("Mensagem inválida recebida: {Message}", messageBody);
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("Mensagem recebida - VideoId: {VideoId}", message.VideoId);

            // // Monta os caminhos baseado nas configurações
            var fileName = $"{message.VideoId}.mp4";
            var videoKey = $"{_storageSettings.InputFolder}/{fileName}";
            var outputZipKey = $"{_storageSettings.OutputFolder}/{message.VideoId}_frames.zip";

            using var scope = _scopeFactory.CreateScope();
            var extractFramesUseCase = scope.ServiceProvider
                .GetRequiredService<IExtractFramesUseCase>();

            var request = new ExtractFramesRequest(
                BucketName: _storageSettings.BucketName,
                VideoKey: videoKey,
                OutputZipKey: outputZipKey
            );

           _logger.LogInformation(
                "Iniciando processamento do vídeo: {VideoId} - {VideoKey}",
                message.VideoId,
                request.VideoKey);
            
            var response = await extractFramesUseCase.ExecuteAsync(request, stoppingToken);

            // Publica mensagem de conclusão
            var completedMessage = new VideoProcessingCompletedMessage
            {
                VideoId = message.VideoId,
                CorrelationId = message.CorrelationId,
                Success = response.Success,
                FramesExtracted = response.FramesExtracted,
                OutputUri = response.OutputUri,
                ProcessingTimeSeconds = response.ProcessingTime.TotalSeconds,
                ErrorMessage = response.ErrorMessage,
                CompletedAt = DateTime.UtcNow
            };

            PublishCompletedMessage(completedMessage);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Processamento concluído com sucesso! VideoId: {VideoId}, {FrameCount} frames extraídos em {Time:F2}s. Output: {OutputUri}",
                    message.VideoId,
                    response.FramesExtracted,
                    response.ProcessingTime.TotalSeconds,
                    response.OutputUri);

                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                _logger.LogError(
                    "Falha no processamento do VideoId: {VideoId} - {ErrorMessage}",
                    message.VideoId,
                    response.ErrorMessage);

                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processamento cancelado.");
            _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar mensagem: {Message}", messageBody);
            _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar vídeo. Mensagem: {Message}", messageBody);

            // Publica mensagem de erro se temos o VideoId
            if (message is not null)
            {
                var errorMessage = new VideoProcessingCompletedMessage
                {
                    VideoId = message.VideoId,
                    CorrelationId = message.CorrelationId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.UtcNow
                };

                PublishCompletedMessage(errorMessage);
            }

            _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private void PublishCompletedMessage(VideoProcessingCompletedMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel?.CreateBasicProperties();
            if (properties is not null)
            {
                properties.Persistent = true;
                properties.ContentType = "application/json";
            }

            _channel?.BasicPublish(
                exchange: string.Empty,
                routingKey: _rabbitMqSettings.CompletedQueueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Mensagem de conclusão publicada - VideoId: {VideoId}, Success: {Success}",
                message.VideoId,
                message.Success);
            }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem de conclusão para VideoId: {VideoId}", message.VideoId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Encerrando Worker...");

        _channel?.Close();
        _connection?.Close();

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("Worker encerrado.");
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
