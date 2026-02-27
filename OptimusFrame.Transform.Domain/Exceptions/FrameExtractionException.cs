namespace OptimusFrame.Transform.Domain.Exceptions;

public class FrameExtractionException : DomainException
{
    public FrameExtractionException(string message) : base(message) { }
    public FrameExtractionException(string message, Exception innerException) : base(message, innerException) { }
}
