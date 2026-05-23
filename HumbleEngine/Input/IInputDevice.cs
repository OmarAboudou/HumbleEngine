namespace HumbleEngine;

public interface IInputDevice
{
    string Name        { get; }
    int    Index       { get; }
    bool   IsConnected { get; }
}
