namespace GitIStage;

internal sealed class ConsoleCommand
{
    private readonly Action _handler;
    private readonly ConsoleKey _key;
    public readonly string Description;
    private readonly ConsoleModifiers _modifiers;

    public ConsoleCommand(Action handler, ConsoleKey key, string description)
    {
        _handler = handler;
        _key = key;
        Description = description;
        _modifiers = 0;
    }

    public ConsoleCommand(Action handler, ConsoleKey key, ConsoleModifiers modifiers, string description)
    {
        _handler = handler;
        _key = key;
        _modifiers = modifiers;

        Description = description;
    }

    public void Execute()
    {
        _handler();
    }

    public bool MatchesKey(ConsoleKeyInfo keyInfo)
    {
        return _key == keyInfo.Key && _modifiers == keyInfo.Modifiers;
    }

    public string GetCommandShortcut()
    {
        string key = string.Empty;

        switch (_key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                key = _key.ToString().Replace("Arrow", "");
                break;
            case ConsoleKey.OemPlus:
                key = "+";
                break;
            case ConsoleKey.OemMinus:
                key = "-";
                break;
            case ConsoleKey.Oem4:
                key = "[";
                break;
            case ConsoleKey.Oem6:
                key = "]";
                break;
            case ConsoleKey.Oem7:
                key = "'";
                break;
            case ConsoleKey.D0:
            case ConsoleKey.D1:
            case ConsoleKey.D2:
            case ConsoleKey.D3:
            case ConsoleKey.D4:
            case ConsoleKey.D5:
            case ConsoleKey.D6:
            case ConsoleKey.D7:
            case ConsoleKey.D8:
            case ConsoleKey.D9:
                key = _key.ToString().Replace("D", "");
                break;
            default:
                key = _key.ToString();
                break;
        }

        if (_key == ConsoleKey.Oem2 && _modifiers == ConsoleModifiers.Shift)
            return "?";

        if (_modifiers != 0)
            return $"{_modifiers.ToString().Replace("Control", "Ctrl")} + {key}";
        else
            return key;
    }
}