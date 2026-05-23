namespace HumbleEngine;

public interface IMonitor
{
    int          Index       { get; }
    string       Name        { get; }
    bool         IsPrimary   { get; }
    Vector2<int> Position    { get; }  // position dans l'espace écran global
    Vector2<int> Size        { get; }  // résolution native
    int          RefreshRate { get; }  // Hz
}
