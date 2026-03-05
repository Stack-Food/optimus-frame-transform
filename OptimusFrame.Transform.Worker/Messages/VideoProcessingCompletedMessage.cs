using System.Text.Json.Serialization;

namespace OptimusFrame.Transform.Worker.Messages;

/// <summary>
/// Mensagem publicada na fila de conclusão após o processamento do vídeo
/// </summary>
public record VideoProcessingCompletedMessage
{
    /// <summary>
    /// ID único do vídeo processado
    /// </summary>
    [JsonPropertyName("videoId")]
    public string VideoId { get; init; } = string.Empty;

    /// <summary>
    /// ID de correlação para rastreamento
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Indica se o processamento foi bem-sucedido
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Quantidade de frames extraídos
    /// </summary>
    [JsonPropertyName("framesExtracted")]
    public int FramesExtracted { get; init; }

    /// <summary>
    /// URI do arquivo ZIP com os frames no S3
    /// </summary>
    [JsonPropertyName("outputUri")]
    public string? OutputUri { get; init; }

    /// <summary>
    /// Tempo de processamento em segundos
    /// </summary>
    [JsonPropertyName("processingTimeSeconds")]
    public double ProcessingTimeSeconds { get; init; }

    /// <summary>
    /// Mensagem de erro (caso Success = false)
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Data/hora de conclusão do processamento (UTC)
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}
