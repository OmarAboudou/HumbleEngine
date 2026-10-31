namespace HumbleEngine.Core;

public abstract class HumbleEngineException : Exception
{
    protected HumbleEngineException
    (
        string? message,
        Exception? innerException
    )
    :
    base
    (
        message,
        innerException
    ) 
    {}

}