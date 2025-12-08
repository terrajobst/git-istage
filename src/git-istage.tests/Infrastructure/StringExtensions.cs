using GitIStage.Patching.Text;

namespace GitIStage.Tests.Infrastructure;

internal static class StringExtensions
{
    public static string Substring(this string s, TextSpan span)
    {
        return s.Substring(span.Start, span.Length);
    }

    public static string Remove(this string s, TextSpan range)
    {
        var beforeSpan = TextSpan.FromBounds(0, range.Start);
        var afterSpan = TextSpan.FromBounds(range.End, s.Length);
        return s.Substring(beforeSpan) + s.Substring(afterSpan);
    }
}