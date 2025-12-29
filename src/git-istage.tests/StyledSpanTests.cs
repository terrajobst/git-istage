using GitIStage.Text;

namespace GitIStage.Tests;

public class StyledSpanTests
{
    [Theory]
    [MemberData(nameof(GetColorPairs))]
    public void StyledSpan_Extracts_ColorsCorrectly(ConsoleColor? foreground, ConsoleColor? background)
    {
        var span = new TextSpan(1, 2);
        var styledSpan = new StyledSpan(span, foreground, background);

        styledSpan.Span.Should().Be(span);
        styledSpan.Foreground.Should().Be(foreground);
        styledSpan.Background.Should().Be(background);
    }

    public static IEnumerable<object?[]> GetColorPairs()
    {
        var colors = Enum
            .GetValues<ConsoleColor>()
            .Cast<ConsoleColor?>()
            .Prepend(null)
            .ToArray();

        foreach (var foreground in colors)
        foreach (var background in colors)
        {
            yield return [foreground, background];
        }
    }
}