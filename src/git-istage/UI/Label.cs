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

    public TextColor? Foreground { get; set; }

    public TextColor? Background { get; set; }

    public string Text { get; set; }

    public void Resize(int top, int left, int right)
    {
        Top = top;
        Left = left;
        Width = right - left;
    }

    public void Render(RenderBuffer buffer)
    {
        if (Width == 0)
            return;

        var textLength = Math.Min(Text.Length, Width);
        var text = Text.AsSpan(0, textLength);

        buffer.SetCursorPosition(Left, Top);
        buffer.SetForegroundColor(Foreground);
        buffer.SetBackgroundColor(Background);
        buffer.Write(text);
        buffer.EraseRestOfCurrentLine();
    }
}
