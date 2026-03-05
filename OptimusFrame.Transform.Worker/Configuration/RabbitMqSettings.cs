namespace OptimusFrame.Transform.Worker.Configuration;

/// <summary>
/// Configurações de conexão com o RabbitMQ
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    /// <summary>
    /// Fila de entrada para receber solicitações de processamento
    /// </summary>
    public string InputQueueName { get; set; } = "video-processing-queue";
    
    /// <summary>
    /// Fila de saída para publicar mensagens de conclusão
    /// </summary>
    public string CompletedQueueName { get; set; } = "video-processing-completed-queue";
    
    public bool AutoAck { get; set; } = false;
    public ushort PrefetchCount { get; set; } = 1;
}
