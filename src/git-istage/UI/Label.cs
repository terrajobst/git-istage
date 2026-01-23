using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class Label
{
    public Label()
    {
        Text = string.Empty;
    }

    public int Top { get; private set; }

    public int Left { get; private set; }

    public int Width { get; private set; }

    public TextColor? Foreground
    {
        get;
        set
        {
            field = value;
            Render();
        }
    }

    public TextColor? Background
    {
        get;
        set
        {
            field = value;
            Render();
        }
    }

    public string Text
    {
        get;
        set
        {
            field = value;
            Render();
        }
    }

    public void Resize(int top, int left, int right)
    {
        Top = top;
        Left = left;
        Width = right - left;

        Render();
    }

    private void Render()
    {
        if (Width == 0)
            return;

        var textLength = Math.Min(Text.Length, Width);
        var text = Text.AsSpan(0, textLength);

        Vt100.SetCursorPosition(Left, Top);
        Vt100.SetForegroundColor(Foreground);
        Vt100.SetBackgroundColor(Background);
        Console.Write(text);
        Vt100.EraseRestOfCurrentLine();
    }
}