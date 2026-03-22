namespace GitIStage.Tests;

public class TextSpanTests
{
    // Constructor and properties

    [Fact]
    public void TextSpan_Constructor_SetsStartAndLength()
    {
        var span = new TextSpan(3, 7);

        span.Start.Should().Be(3);
        span.Length.Should().Be(7);
        span.End.Should().Be(10);
    }

    [Fact]
    public void TextSpan_Constructor_ZeroLength_IsValid()
    {
        var span = new TextSpan(5, 0);

        span.Start.Should().Be(5);
        span.Length.Should().Be(0);
        span.End.Should().Be(5);
    }

    [Fact]
    public void TextSpan_Constructor_NegativeStart_Throws()
    {
        var act = () => new TextSpan(-1, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // FromBounds

    [Fact]
    public void TextSpan_FromBounds_ComputesLength()
    {
        var span = TextSpan.FromBounds(3, 10);

        span.Start.Should().Be(3);
        span.Length.Should().Be(7);
        span.End.Should().Be(10);
    }

    [Fact]
    public void TextSpan_FromBounds_SameStartAndEnd_CreatesZeroLengthSpan()
    {
        var span = TextSpan.FromBounds(5, 5);

        span.Start.Should().Be(5);
        span.Length.Should().Be(0);
    }

    [Fact]
    public void TextSpan_FromBounds_NegativeStart_Throws()
    {
        var act = () => TextSpan.FromBounds(-1, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TextSpan_FromBounds_EndBeforeStart_Throws()
    {
        var act = () => TextSpan.FromBounds(10, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // RelativeTo

    [Fact]
    public void TextSpan_RelativeTo_SubtractsPosition()
    {
        var span = new TextSpan(10, 5);

        var relative = span.RelativeTo(7);

        relative.Start.Should().Be(3);
        relative.Length.Should().Be(5);
    }

    [Fact]
    public void TextSpan_RelativeTo_Zero_ReturnsSameSpan()
    {
        var span = new TextSpan(10, 5);

        var relative = span.RelativeTo(0);

        relative.Should().Be(span);
    }

    // OverlapsWith

    [Fact]
    public void TextSpan_OverlapsWith_OverlappingSpans_ReturnsTrue()
    {
        var a = new TextSpan(0, 10);
        var b = new TextSpan(5, 10);

        a.OverlapsWith(b).Should().BeTrue();
        b.OverlapsWith(a).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_OverlapsWith_ContainedSpan_ReturnsTrue()
    {
        var outer = new TextSpan(0, 10);
        var inner = new TextSpan(3, 4);

        outer.OverlapsWith(inner).Should().BeTrue();
        inner.OverlapsWith(outer).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_OverlapsWith_AdjacentSpans_ReturnsFalse()
    {
        var a = new TextSpan(0, 5);
        var b = new TextSpan(5, 5);

        a.OverlapsWith(b).Should().BeFalse();
        b.OverlapsWith(a).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_OverlapsWith_DisjointSpans_ReturnsFalse()
    {
        var a = new TextSpan(0, 3);
        var b = new TextSpan(5, 3);

        a.OverlapsWith(b).Should().BeFalse();
        b.OverlapsWith(a).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_OverlapsWith_ZeroLengthInsideSpan_ReturnsTrue()
    {
        var a = new TextSpan(0, 10);
        var b = new TextSpan(5, 0);

        a.OverlapsWith(b).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_OverlapsWith_ZeroLengthAtEnd_ReturnsFalse()
    {
        var a = new TextSpan(0, 5);
        var b = new TextSpan(5, 0);

        a.OverlapsWith(b).Should().BeFalse();
    }

    // Contains(TextSpan)

    [Fact]
    public void TextSpan_ContainsSpan_FullyContained_ReturnsTrue()
    {
        var outer = new TextSpan(0, 10);
        var inner = new TextSpan(2, 5);

        outer.Contains(inner).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_ContainsSpan_SameSpan_ReturnsTrue()
    {
        var span = new TextSpan(3, 5);

        span.Contains(span).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_ContainsSpan_PartialOverlap_ReturnsFalse()
    {
        var a = new TextSpan(0, 5);
        var b = new TextSpan(3, 5);

        a.Contains(b).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_ContainsSpan_Disjoint_ReturnsFalse()
    {
        var a = new TextSpan(0, 5);
        var b = new TextSpan(10, 3);

        a.Contains(b).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_ContainsSpan_ZeroLengthAtStart_ReturnsTrue()
    {
        var outer = new TextSpan(0, 10);
        var inner = new TextSpan(0, 0);

        outer.Contains(inner).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_ContainsSpan_ZeroLengthAtEnd_ReturnsFalse()
    {
        var outer = new TextSpan(0, 10);
        var inner = new TextSpan(10, 0);

        outer.Contains(inner).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_ContainsSpan_ZeroLengthInside_ReturnsTrue()
    {
        var outer = new TextSpan(0, 10);
        var inner = new TextSpan(5, 0);

        outer.Contains(inner).Should().BeTrue();
    }

    // Contains(int)

    [Fact]
    public void TextSpan_ContainsPosition_InsideSpan_ReturnsTrue()
    {
        var span = new TextSpan(5, 10);

        span.Contains(5).Should().BeTrue();
        span.Contains(10).Should().BeTrue();
        span.Contains(14).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_ContainsPosition_AtEnd_ReturnsFalse()
    {
        var span = new TextSpan(5, 10);

        span.Contains(15).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_ContainsPosition_BeforeStart_ReturnsFalse()
    {
        var span = new TextSpan(5, 10);

        span.Contains(4).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_ContainsPosition_NegativePosition_Throws()
    {
        var span = new TextSpan(5, 10);

        var act = () => span.Contains(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // Equality

    [Fact]
    public void TextSpan_Equals_SameValues_ReturnsTrue()
    {
        var a = new TextSpan(3, 7);
        var b = new TextSpan(3, 7);

        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void TextSpan_Equals_DifferentStart_ReturnsFalse()
    {
        var a = new TextSpan(3, 7);
        var b = new TextSpan(4, 7);

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void TextSpan_Equals_DifferentLength_ReturnsFalse()
    {
        var a = new TextSpan(3, 7);
        var b = new TextSpan(3, 8);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void TextSpan_Equals_NonTextSpanObject_ReturnsFalse()
    {
        var span = new TextSpan(3, 7);

        span.Equals("not a span").Should().BeFalse();
    }

    // ToString

    [Fact]
    public void TextSpan_ToString_ReturnsStartDotDotEnd()
    {
        var span = new TextSpan(3, 7);

        span.ToString().Should().Be("3..10");
    }
}
