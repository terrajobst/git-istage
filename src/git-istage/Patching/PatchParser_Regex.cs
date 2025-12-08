using System.Text.RegularExpressions;

namespace GitIStage.Patching;

internal sealed partial class PatchParser
{
    // Entry

    [GeneratedRegex("^diff --git a/(?<OldPath>.*) b/(?<NewPath>.*)$")]
    private static partial Regex DiffGitRegex();

    [GeneratedRegex(@"^--- a/(?<Path>.+)$|^--- /dev/null$")]
    private static partial Regex OldPathRegex();

    [GeneratedRegex(@"^\+\+\+ b/(?<Path>.+)|^\+\+\+ /dev/null$$")]
    private static partial Regex NewPathRegex();

    [GeneratedRegex("^old mode (?<Mode>[0-7]{1,6})$")]
    private static partial Regex OldModeRegex();

    [GeneratedRegex("^new mode (?<Mode>[0-7]{1,6})$")]
    private static partial Regex NewModeRegex();

    [GeneratedRegex("^deleted file mode (?<Mode>[0-7]{1,6})$")]
    private static partial Regex DeletedFileModeRegex();

    [GeneratedRegex("^new file mode (?<Mode>[0-7]{1,6})$")]
    private static partial Regex NewFileModeRegex();

    [GeneratedRegex("^copy from (?<Path>.+)$")]
    private static partial Regex CopyFromRegex();

    [GeneratedRegex("^copy to (?<Path>.+)$")]
    private static partial Regex CopyToRegex();

    [GeneratedRegex("^rename from (?<Path>.+)$")]
    private static partial Regex RenameFromRegex();

    [GeneratedRegex("^rename to (?<Path>.+)$")]
    private static partial Regex RenameToRegex();

    [GeneratedRegex("^similarity index (?<Number>[0-9]+)%$")]
    private static partial Regex SimilarityIndexRegex();

    [GeneratedRegex("^dissimilarity index (?<Number>[0-9]+)%$")]
    private static partial Regex DissimilarityIndexRegex();

    [GeneratedRegex("^index (?<Hash1>[0-9a-fA-F]+)..(?<Hash2>[0-9a-fA-F]+)( (?<Mode>[0-7]{1,6}))?$")]
    private static partial Regex IndexRegex();

    // Hunk

    [GeneratedRegex(@"^@@ -(?<OldStart>[0-9]+)(,(?<OldLength>[0-9]+))? \+(?<NewStart>[0-9]+)(,(?<NewLength>[0-9]+))? @@( (?<Function>.+))?$")]
    private static partial Regex HunkRegex();

    [GeneratedRegex(@"^ (?<Text>.*)|^$")]
    private static partial Regex ContextRegex();

    [GeneratedRegex(@"^\+(?<Text>.*)")]
    private static partial Regex AddedRegex();

    [GeneratedRegex(@"^-(?<Text>.*)")]
    private static partial Regex DeletedRegex();

    [GeneratedRegex(@"^\\ (?<Text>.*)")]
    private static partial Regex NoNewLineRegex();

    // Fallback

    [GeneratedRegex(@"^.*$")]
    private static partial Regex UnknownRegex();

    // End of File

    [GeneratedRegex(@"$")]
    private static partial Regex EndOfFile();
}