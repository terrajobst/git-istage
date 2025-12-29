namespace GitIStage.Patches;

internal static class PatchError
{
    public static FormatException ExpectedDiffGitHeader(int lineNumber)
    {
        return new FormatException($"Invalid patch. Expected 'diff --git' in line {lineNumber}.");
    }

    public static FormatException ExpectedLine(int lineNumber, string lineName)
    {
        return new FormatException($"Invalid patch. Expected {lineName} in line {lineNumber}");
    }

    public static FormatException ExpectedPercentage(int lineNumber, int column, int number)
    {
        return new FormatException($"Invalid patch. Expected percentage at {lineNumber}:{column} but found '{number}'");
    }

    public static FormatException ExpectedInt32(int lineNumber, int column, ReadOnlySpan<char> text)
    {
        return new FormatException($"Invalid patch. Expected integer at {lineNumber}:{column} but found '{text}'");
    }

    public static FormatException ExpectedMode(int lineNumber, int column, ReadOnlySpan<char> text)
    {
        return new FormatException($"Invalid patch. Expected mode at {lineNumber}:{column} but found '{text}'");
    }
}