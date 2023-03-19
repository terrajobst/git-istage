namespace GitIStage.UI;

internal sealed class Label
{
    private readonly int _top;
    private readonly int _left;
    private readonly int _width;

    private ConsoleColor? _foreground = null;
    private ConsoleColor? _background = null;
    private string _text = string.Empty;

    public Label(int top, int left, int right)
    {
        _top = top;
        _left = left;
        _width = right - left;
    }

    public ConsoleColor? Foreground
    {
        get => _foreground;
        set
        {
            _foreground = value;
            Render();
        }
    }

    public ConsoleColor? Background
    {
        get => _background;
        set
        {
            _background = value;
            Render();
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            Render();
        }
    }

    private void Render()
    {
        var textLength = Math.Min(_text.Length, _width);
        var text = _text.Substring(0, textLength);

        Vt100.SetCursorPosition(_left, _top);
        Vt100.SetForegroundColor(_foreground);
        Vt100.SetBackgroundColor(_background);
        Vt100.EraseRestOfCurrentLine();
        Console.Write(text);
    }
}