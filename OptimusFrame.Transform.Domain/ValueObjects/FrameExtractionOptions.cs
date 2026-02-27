namespace OptimusFrame.Transform.Domain.ValueObjects;

/// <summary>
/// Opções para extração de frames
/// </summary>
public record FrameExtractionOptions
{
    /// <summary>
    /// Formato de saída das imagens (png, jpg, webp)
    /// </summary>
    public ImageFormat Format { get; init; } = ImageFormat.Jpeg;

    /// <summary>
    /// Qualidade da imagem (1-100). Aplicável para JPEG e WebP
    /// </summary>
    public int Quality { get; init; } = 80;

    /// <summary>
    /// Frames por segundo a extrair. Se null, extrai todos os frames
    /// Exemplo: 1 = 1 frame por segundo, 0.5 = 1 frame a cada 2 segundos
    /// </summary>
    public double? TargetFps { get; init; } = 1;

    /// <summary>
    /// Escala de redimensionamento (0.1 a 1.0). Exemplo: 0.5 = metade do tamanho
    /// </summary>
    public double Scale { get; init; } = 1.0;

    /// <summary>
    /// Largura máxima em pixels. Mantém proporção se apenas Width ou Height for definido
    /// </summary>
    public int? MaxWidth { get; init; }

    /// <summary>
    /// Altura máxima em pixels. Mantém proporção se apenas Width ou Height for definido
    /// </summary>
    public int? MaxHeight { get; init; }

    /// <summary>
    /// Opções padrão otimizadas para menor tamanho
    /// </summary>
    public static FrameExtractionOptions Optimized => new()
    {
        Format = ImageFormat.Jpeg,
        Quality = 75,
        TargetFps = 1,
        MaxWidth = 1280
    };

    /// <summary>
    /// Opções para máxima qualidade (todos os frames, PNG)
    /// </summary>
    public static FrameExtractionOptions HighQuality => new()
    {
        Format = ImageFormat.Png,
        Quality = 100,
        TargetFps = null,
        Scale = 1.0
    };
}

/// <summary>
/// Formato de imagem para os frames extraídos
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// PNG - Sem perdas, maior tamanho
    /// </summary>
    Png,

    /// <summary>
    /// JPEG - Com perdas, menor tamanho (recomendado)
    /// </summary>
    Jpeg,

    /// <summary>
    /// WebP - Melhor compressão, menor compatibilidade
    /// </summary>
    WebP
}