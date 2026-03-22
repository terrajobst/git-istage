namespace GitIStage.Tests;

public class TextStyleTests
{
    [Fact]
    public void TextStyle_Default_HasNoForegroundBackgroundOrAttributes()
    {
        var style = new TextStyle();

        style.Foreground.Should().BeNull();
        style.Background.Should().BeNull();
        style.Attributes.Should().Be(TextAttributes.None);
    }

    [Fact]
    public void TextStyle_Equals_WhenSame()
    {
        var style1 = new TextStyle
        {
            Foreground = new TextColor(255, 0, 0),
            Background = new TextColor(0, 0, 255),
            Attributes = TextAttributes.Bold
        };

        var style2 = new TextStyle
        {
            Foreground = new TextColor(255, 0, 0),
            Background = new TextColor(0, 0, 255),
            Attributes = TextAttributes.Bold
        };

        style1.Should().Be(style2);
        (style1 == style2).Should().BeTrue();
        (style1 != style2).Should().BeFalse();
        style1.GetHashCode().Should().Be(style2.GetHashCode());
    }

    [Fact]
    public void TextStyle_NotEquals_WhenForegroundDiffers()
    {
        var style1 = new TextStyle { Foreground = new TextColor(255, 0, 0) };
        var style2 = new TextStyle { Foreground = new TextColor(0, 255, 0) };

        style1.Should().NotBe(style2);
        (style1 != style2).Should().BeTrue();
    }

    [Fact]
    public void TextStyle_NotEquals_WhenBackgroundDiffers()
    {
        var style1 = new TextStyle { Background = new TextColor(255, 0, 0) };
        var style2 = new TextStyle { Background = new TextColor(0, 255, 0) };

        style1.Should().NotBe(style2);
    }

    [Fact]
    public void TextStyle_NotEquals_WhenAttributesDiffer()
    {
        var style1 = new TextStyle { Attributes = TextAttributes.Bold };
        var style2 = new TextStyle { Attributes = TextAttributes.Italic };

        style1.Should().NotBe(style2);
    }

    [Fact]
    public void TextStyle_PlaceOnTopOf_InheritsForeground_WhenNull()
    {
        var previous = new TextStyle { Foreground = new TextColor(255, 0, 0) };
        var current = new TextStyle();

        var result = current.PlaceOnTopOf(previous);

        result.Foreground.Should().Be(new TextColor(255, 0, 0));
    }

    [Fact]
    public void TextStyle_PlaceOnTopOf_InheritsBackground_WhenNull()
    {
        var previous = new TextStyle { Background = new TextColor(0, 0, 255) };
        var current = new TextStyle();

        var result = current.PlaceOnTopOf(previous);

        result.Background.Should().Be(new TextColor(0, 0, 255));
    }

    [Fact]
    public void TextStyle_PlaceOnTopOf_CombinesForeground_WhenBothSet()
    {
        var previous = new TextStyle { Foreground = new TextColor(100, 100, 100) };
        var current = new TextStyle { Foreground = new TextColor(200, 200, 200) };

        var result = current.PlaceOnTopOf(previous);

        result.Foreground.Should().NotBeNull();
        result.Foreground.Should().Be(new TextColor(100, 100, 100).Combine(new TextColor(200, 200, 200)));
    }

    [Fact]
    public void TextStyle_PlaceOnTopOf_SetsForeground_WhenPreviousIsNull()
    {
        var previous = new TextStyle();
        var current = new TextStyle { Foreground = new TextColor(200, 200, 200) };

        var result = current.PlaceOnTopOf(previous);

        result.Foreground.Should().Be(new TextColor(200, 200, 200));
    }

    [Fact]
    public void TextStyle_PlaceOnTopOf_MergesAttributes()
    {
        var previous = new TextStyle { Attributes = TextAttributes.Bold };
        var current = new TextStyle { Attributes = TextAttributes.Italic };

        var result = current.PlaceOnTopOf(previous);

        result.Attributes.Should().Be(TextAttributes.Bold | TextAttributes.Italic);
    }
}
