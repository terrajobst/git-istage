namespace GitIStage.UI;

internal sealed class Label
{
    private int _top;
    private int _left;
    private int _width;

    private ConsoleColor? _foreground;
    private ConsoleColor? _background;
    private string _text = string.Empty;

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

    public void Resize(int top, int left, int right)
    {
        _top = top;
        _left = left;
        _width = right - left;

        Render();
    }
    
    private void Render()
    {
        if (_width == 0)
            return;

        var textLength = Math.Min(_text.Length, _width);
        var text = _text.Substring(0, textLength);

        Vt100.SetCursorPosition(_left, _top);
        Vt100.SetForegroundColor(_foreground);
        Vt100.SetBackgroundColor(_background);
        Console.Write(text);
        Vt100.EraseRestOfCurrentLine();
    }
}