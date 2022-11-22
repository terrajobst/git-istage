namespace GitIStage.Commands;

internal sealed class ConsoleCommand
{
    private readonly string _name;
    private readonly Action _handler;
    private readonly IReadOnlyList<ConsoleKeyBinding> _keyBindings;
    private readonly string _description;

    public ConsoleCommand(string name, Action handler, IReadOnlyList<ConsoleKeyBinding> keyBindings, string description)
    {
        _name = name;
        _handler = handler;
        _keyBindings = keyBindings;
        _description = description;
    }

    public string Name => _name;

    public IReadOnlyList<ConsoleKeyBinding> KeyBindings => _keyBindings;

    public string Description => _description;

    public void Execute()
    {
        _handler();
    }

    public ConsoleCommand WithKeyBindings(IReadOnlyList<ConsoleKeyBinding> value)
    {
        return new ConsoleCommand(_name, _handler, value, _description);
    }
}
