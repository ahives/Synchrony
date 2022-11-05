namespace Synchrony;

using System.Runtime.Serialization;

public class SynchronyConfigurationException : Exception
{
    public SynchronyConfigurationException()
    {
    }

    protected SynchronyConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public SynchronyConfigurationException(string? message) : base(message)
    {
    }

    public SynchronyConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}