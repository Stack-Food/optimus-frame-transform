using System.Text.Json.Serialization;

namespace OptimusFrame.Transform.Worker.Messages;

/// <summary>
/// Mensagem recebida da fila RabbitMQ com informações do vídeo a ser processado
/// </summary>
public record VideoProcessingMessage
{
    /// <summary>
    /// ID único do vídeo a ser processado
    /// </summary>
    [JsonPropertyName("videoId")]
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// Nome do arquivo do vídeo (opcional - se não informado, usa VideoId)
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }

    /// <summary>
    /// ID de correlação para rastreamento (opcional)
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}
