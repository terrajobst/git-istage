namespace GitIStage.Tests;

public class StyledSpanTests
{
    [Fact]
    public void StyledSpan_ThreeArgConstructor_SetsProperties()
    {
        var span = new TextSpan(5, 10);
        var fg = new TextColor(255, 0, 0);
        var bg = new TextColor(0, 0, 255);

        var styledSpan = new StyledSpan(span, fg, bg);

        styledSpan.Span.Should().Be(span);
        styledSpan.Foreground.Should().Be(fg);
        styledSpan.Background.Should().Be(bg);
    }

    [Fact]
    public void StyledSpan_TwoArgConstructor_SetsProperties()
    {
        var span = new TextSpan(5, 10);
        var style = new TextStyle
        {
            Foreground = new TextColor(255, 0, 0),
            Background = new TextColor(0, 0, 255),
            Attributes = TextAttributes.Bold
        };

        var styledSpan = new StyledSpan(span, style);

        styledSpan.Span.Should().Be(span);
        styledSpan.Foreground.Should().Be(style.Foreground);
        styledSpan.Background.Should().Be(style.Background);
        styledSpan.Style.Attributes.Should().Be(TextAttributes.Bold);
    }

    [Fact]
    public void StyledSpan_BothConstructors_AreEqual_WhenSameForegroundAndBackground()
    {
        var span = new TextSpan(0, 5);
        var fg = new TextColor(10, 20, 30);
        var bg = new TextColor(40, 50, 60);

        var fromThreeArg = new StyledSpan(span, fg, bg);
        var fromTwoArg = new StyledSpan(span, new TextStyle { Foreground = fg, Background = bg });

        fromThreeArg.Should().Be(fromTwoArg);
        fromThreeArg.GetHashCode().Should().Be(fromTwoArg.GetHashCode());
    }

    [Fact]
    public void StyledSpan_NotEqual_WhenSpansDiffer()
    {
        var fg = new TextColor(255, 0, 0);
        var s1 = new StyledSpan(new TextSpan(0, 5), fg, null);
        var s2 = new StyledSpan(new TextSpan(1, 5), fg, null);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void StyledSpan_NotEqual_WhenAttributesDiffer()
    {
        var span = new TextSpan(0, 5);
        var s1 = new StyledSpan(span, new TextStyle { Attributes = TextAttributes.Bold });
        var s2 = new StyledSpan(span, new TextStyle { Attributes = TextAttributes.Italic });

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void StyledSpan_Operators_Work()
    {
        var s1 = new StyledSpan(new TextSpan(0, 5), new TextColor(1, 2, 3), null);
        var s2 = new StyledSpan(new TextSpan(0, 5), new TextColor(1, 2, 3), null);
        var s3 = new StyledSpan(new TextSpan(0, 6), new TextColor(1, 2, 3), null);

        (s1 == s2).Should().BeTrue();
        (s1 != s3).Should().BeTrue();
    }
}
