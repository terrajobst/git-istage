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
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(0, 5), PatchClassification.Keyword),
            new ClassifiedSpan(new TextSpan(6, 5), PatchClassification.Keyword),
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
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(0, 4), PatchClassification.Keyword),
            new ClassifiedSpan(new TextSpan(9, 4), PatchClassification.Keyword),
            new ClassifiedSpan(new TextSpan(18, 4), PatchClassification.Keyword),
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
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(4, 3), PatchClassification.Keyword),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(2);
        highlights[0].Should().BeEmpty();
        highlights[1].Should().HaveCount(1);
        highlights[1][0].Span.Start.Should().Be(0);
        highlights[1][0].Span.Length.Should().Be(3);
    }

    [Fact]
    public void LineHighlights_Create_WithOffset_StartsFromOffsetLine()
    {
        var text = SourceText.From("aaa\nbbb\nccc");
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(4, 3), PatchClassification.Keyword),
        };

        var highlights = LineHighlights.Create(text, spans, offset: 4);

        highlights.Count.Should().Be(1);
        highlights[0].Should().HaveCount(1);
        highlights[0][0].Span.Start.Should().Be(0);
        highlights[0][0].Span.Length.Should().Be(3);
    }

    [Fact]
    public void LineHighlights_Create_PreservesClassification()
    {
        var text = SourceText.From("test");
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(0, 4), PatchClassification.PathToken),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights[0][0].Classification.Should().Be(PatchClassification.PathToken);
    }

    [Fact]
    public void LineHighlights_Create_PreservesScopesClassification()
    {
        var text = SourceText.From("test");
        var classification = Classification.Create("source.cs", "keyword.control");
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(0, 4), classification),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights[0][0].Classification.Should().Be(classification);
        highlights[0][0].Classification.Scopes.Length.Should().Be(2);
    }

    [Fact]
    public void LineHighlights_Create_MultipleSpansOnSameLine_PreservesOrder()
    {
        var text = SourceText.From("hello world test");
        var spans = new[]
        {
            new ClassifiedSpan(new TextSpan(0, 5), PatchClassification.Keyword),
            new ClassifiedSpan(new TextSpan(6, 5), PatchClassification.Operator),
            new ClassifiedSpan(new TextSpan(12, 4), PatchClassification.PathToken),
        };

        var highlights = LineHighlights.Create(text, spans);

        highlights.Count.Should().Be(1);
        highlights[0].Should().HaveCount(3);
        highlights[0][0].Classification.Should().Be(PatchClassification.Keyword);
        highlights[0][1].Classification.Should().Be(PatchClassification.Operator);
        highlights[0][2].Classification.Should().Be(PatchClassification.PathToken);
    }
}
