namespace GitIStage.Commands;

internal sealed class ConsoleCommand
{
    private readonly Action _handler;

    public ConsoleCommand(string name, Action handler, IReadOnlyList<ConsoleKeyBinding> keyBindings, string description)
    {
        Name = name;
        _handler = handler;
        KeyBindings = keyBindings;
        Description = description;
    }

    public string Name { get; }

    public IReadOnlyList<ConsoleKeyBinding> KeyBindings { get; }

    public string Description { get; }

    public void Execute()
    {
        _handler();
    }

    public ConsoleCommand WithKeyBindings(IReadOnlyList<ConsoleKeyBinding> value)
    {
        return new ConsoleCommand(Name, _handler, value, Description);
    }
}
