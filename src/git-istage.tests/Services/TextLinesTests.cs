using System.Text;

namespace GitIStage.Tests;

public class TextLinesTests
{
    [Fact]
    public void TextLines_Empty_HasZeroCount()
    {
        TextLines.Empty.Count.Should().Be(0);
    }

    [Fact]
    public void TextLines_FromStream_ReadsLines()
    {
        var content = "line one\nline two\nline three";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var lines = TextLines.FromStream(stream);

        lines.Count.Should().Be(3);
        lines[0].Span.SequenceEqual("line one".AsSpan()).Should().BeTrue();
        lines[1].Span.SequenceEqual("line two".AsSpan()).Should().BeTrue();
        lines[2].Span.SequenceEqual("line three".AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void TextLines_FromStream_EmptyStream_ReturnsEmpty()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var lines = TextLines.FromStream(stream);

        lines.Count.Should().Be(0);
    }

    [Fact]
    public void TextLines_FromStream_WindowsLineEndings_ReadsLines()
    {
        var content = "line one\r\nline two\r\nline three";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var lines = TextLines.FromStream(stream);

        lines.Count.Should().Be(3);
        lines[0].Span.SequenceEqual("line one".AsSpan()).Should().BeTrue();
        lines[1].Span.SequenceEqual("line two".AsSpan()).Should().BeTrue();
        lines[2].Span.SequenceEqual("line three".AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void TextLines_IsEnumerable()
    {
        var content = "a\nb\nc";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var lines = TextLines.FromStream(stream);
        var list = lines.ToList();

        list.Should().HaveCount(3);
    }

    [Fact]
    public void TextLines_ApplyReversed_WithNull_ReturnsSelf()
    {
        var content = "hello\nworld";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var lines = TextLines.FromStream(stream);

        var result = lines.ApplyReversed(null);

        result.Should().BeSameAs(lines);
    }

    [Fact]
    public void TextLines_ApplyReversed_ReversesAddition()
    {
        // Original file has 3 lines: a, b, c
        // Patch adds a line "x" after line 1 so the new file would be: a, x, b, c
        // ApplyReversed on the "new" lines should give back the old lines
        var patch = Patch.Parse("""
                                diff --git a/test.txt b/test.txt
                                index 1234567..abcdef0 100644
                                --- a/test.txt
                                +++ b/test.txt
                                @@ -1,3 +1,4 @@
                                 a
                                +x
                                 b
                                 c
                                """);

        var entry = patch.Entries[0];

        // The "new" file content (after patch was applied)
        var content = "a\nx\nb\nc";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var newLines = TextLines.FromStream(stream);

        var oldLines = newLines.ApplyReversed(entry);

        oldLines.Count.Should().Be(3);
        oldLines[0].Span.SequenceEqual("a".AsSpan()).Should().BeTrue();
        oldLines[1].Span.SequenceEqual("b".AsSpan()).Should().BeTrue();
        oldLines[2].Span.SequenceEqual("c".AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void TextLines_ApplyReversed_ReversesDeletion()
    {
        // Original file has 3 lines: a, b, c
        // Patch deletes line "b" so the new file would be: a, c
        // ApplyReversed on the "new" lines should give back the old lines
        var patch = Patch.Parse("""
                                diff --git a/test.txt b/test.txt
                                index 1234567..abcdef0 100644
                                --- a/test.txt
                                +++ b/test.txt
                                @@ -1,3 +1,2 @@
                                 a
                                -b
                                 c
                                """);

        var entry = patch.Entries[0];

        // The "new" file content (after patch was applied: b was removed)
        var content = "a\nc";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var newLines = TextLines.FromStream(stream);

        var oldLines = newLines.ApplyReversed(entry);

        oldLines.Count.Should().Be(3);
        oldLines[0].Span.SequenceEqual("a".AsSpan()).Should().BeTrue();
        oldLines[1].Span.SequenceEqual("b".AsSpan()).Should().BeTrue();
        oldLines[2].Span.SequenceEqual("c".AsSpan()).Should().BeTrue();
    }
}
