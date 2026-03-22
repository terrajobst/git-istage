namespace GitIStage.Tests;

public class LineHighlightsTests
{
    [Fact]
    public void LineHighlights_Empty_HasZeroCount()
    {
        LineHighlights.Empty.Count.Should().Be(0);
    }

    [Fact]
    public void LineHighlights_Create_SingleLineSpan_ProducesSingleLine()
    {
        var text = SourceText.From("hello world");
        var fg = new TextColor(255, 0, 0);
        var spans = new[]
        {
            new StyledSpan(new TextSpan(0, 5), fg, null),
            new StyledSpan(new TextSpan(6, 5), fg, null),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(1);
        highlights[0].Should().HaveCount(2);
        highlights[0][0].Span.Should().Be(new TextSpan(0, 5));
        highlights[0][1].Span.Should().Be(new TextSpan(6, 5));
    }

    [Fact]
    public void LineHighlights_Create_MultiLineSpans_GroupsByLine()
    {
        var text = SourceText.From("line one\nline two\nline three");
        var fg = new TextColor(0, 255, 0);
        var spans = new[]
        {
            new StyledSpan(new TextSpan(0, 4), fg, null),   // "line" on line 0
            new StyledSpan(new TextSpan(9, 4), fg, null),   // "line" on line 1
            new StyledSpan(new TextSpan(18, 4), fg, null),  // "line" on line 2
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(3);
        highlights[0].Should().HaveCount(1);
        highlights[1].Should().HaveCount(1);
        highlights[2].Should().HaveCount(1);
    }

    [Fact]
    public void LineHighlights_Create_AdjustsSpansToLineRelative()
    {
        var text = SourceText.From("abc\ndef");
        var fg = new TextColor(0, 0, 255);
        var spans = new[]
        {
            new StyledSpan(new TextSpan(4, 3), fg, null),  // "def" on line 1, starts at offset 4
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(2);
        highlights[0].Should().BeEmpty();
        highlights[1].Should().HaveCount(1);
        highlights[1][0].Span.Start.Should().Be(0);  // adjusted to line-relative
        highlights[1][0].Span.Length.Should().Be(3);
    }

    [Fact]
    public void LineHighlights_Create_WithOffset_StartsFromOffsetLine()
    {
        var text = SourceText.From("aaa\nbbb\nccc");
        var fg = new TextColor(128, 128, 128);
        var spans = new[]
        {
            new StyledSpan(new TextSpan(4, 3), fg, null),  // "bbb" on line 1
        };

        var highlights = LineHighlights.Create(text, spans, offset: 4);

        highlights.Count.Should().Be(1);
        highlights[0].Should().HaveCount(1);
        highlights[0][0].Span.Start.Should().Be(0);
        highlights[0][0].Span.Length.Should().Be(3);
    }

    [Fact]
    public void LineHighlights_Create_PreservesStyle()
    {
        var text = SourceText.From("test");
        var style = new TextStyle
        {
            Foreground = new TextColor(10, 20, 30),
            Background = new TextColor(40, 50, 60),
            Attributes = TextAttributes.Italic
        };
        var spans = new[]
        {
            new StyledSpan(new TextSpan(0, 4), style),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights[0][0].Style.Should().Be(style);
    }

    [Fact]
    public void LineHighlights_Create_MultipleSpansOnSameLine_PreservesOrder()
    {
        var text = SourceText.From("hello world test");
        var fg1 = new TextColor(255, 0, 0);
        var fg2 = new TextColor(0, 255, 0);
        var fg3 = new TextColor(0, 0, 255);
        var spans = new[]
        {
            new StyledSpan(new TextSpan(0, 5), fg1, null),
            new StyledSpan(new TextSpan(6, 5), fg2, null),
            new StyledSpan(new TextSpan(12, 4), fg3, null),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(1);
        highlights[0].Should().HaveCount(3);
        highlights[0][0].Foreground.Should().Be(fg1);
        highlights[0][1].Foreground.Should().Be(fg2);
        highlights[0][2].Foreground.Should().Be(fg3);
    }
}
