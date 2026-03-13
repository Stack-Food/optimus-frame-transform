namespace OptimusFrame.Transform.Worker.Configuration;

/// <summary>
/// Configurações de armazenamento S3
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Nome do bucket S3
    /// </summary>
    public string BucketName { get; set; } = "optimus-frame-core-bucket";

    /// <summary>
    /// Pasta de entrada onde os vídeos estão armazenados
    /// </summary>
    public string InputFolder { get; set; } = "input";

    /// <summary>
    /// Pasta de saída onde os ZIPs com frames serão salvos
    /// </summary>
    public string OutputFolder { get; set; } = "output";
}
