namespace GitIStage.Commands;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class CommandHandlerAttribute : Attribute
{
    public CommandHandlerAttribute(string description, params string[] keyBindings)
    {
        Description = description;
        KeyBindings = keyBindings;
    }
    
    public string Description { get; }

    public string[] KeyBindings { get; }

    public IReadOnlyList<ConsoleKeyBinding> GetKeyBindings()
    {
        return KeyBindings == null
            ? Array.Empty<ConsoleKeyBinding>()
            : KeyBindings.Select(ConsoleKeyBinding.Parse).ToArray();
    }
}