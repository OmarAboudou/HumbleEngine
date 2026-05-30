namespace HumbleEngine;

public sealed class PaintCommandBuffer
{
    private readonly List<PaintCommand> _commands = [];
    public IReadOnlyList<PaintCommand> Commands => _commands;

    public void Add(PaintCommand command) => _commands.Add(command);
    public void Clear() => _commands.Clear();
}
