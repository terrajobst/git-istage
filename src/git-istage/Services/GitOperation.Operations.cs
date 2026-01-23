using GitIStage.Patches;

namespace GitIStage.Services;

partial class GitOperation
{
    public static GitOperation Reset(string path)
    {
        return new GitOperation("reset")
            .AddPath(path)
            .AddAffectedFile(path);
    }

    public static GitOperation Add(string path)
    {
        return new GitOperation("add")
            .AddPath(path)
            .AddAffectedFile(path);
    }

    public static GitOperation Remove(string path, bool force)
    {
        return new GitOperation("rm")
            .AddOptionIf("-f", force)
            .AddPath(path)
            .AddAffectedFile(path);
    }

    public static GitOperation Checkout(string path)
    {
        return new GitOperation("checkout")
            .AddPath(path)
            .AddAffectedFile(path);
    }

    public static GitOperation Restore(bool staged, string path)
    {
        return new GitOperation("restore")
            .AddOptionIf("--staged", staged)
            .AddPath(path)
            .AddAffectedFile(path);
    }

    public static GitOperation Stash(bool untracked, bool keepIndex)
    {
        return new GitOperation("stash")
            .AddOptionIf("-u", untracked)
            .AddOptionIf("-k", keepIndex)
            .AddAffectedFile("*");
    }

    public static GitOperation Commit(bool verbose, bool amend)
    {
        return new GitOperation("commit")
            .AddOptionIf("--amend", amend)
            .AddOptionIf("-v", verbose)
            .AddAffectedFile("*");
    }

    public static GitOperation Apply(Patch patch, bool reverse, bool cached, bool verbose)
    {
        var tempFile = TempFile.CreateWithExtension(".patch", () => patch.ToString());
        var affectedPaths = patch.Entries
            .SelectMany(e => new[] { e.OldPath, e.NewPath })
            .ToHashSet(StringComparer.Ordinal);

        return new GitOperation("apply")
            .AddOptionIf("--reverse", reverse)
            .AddOptionIf("--cached", cached)
            .AddOptionIf("-v", verbose)
            .AddOption("--whitespace=nowarn")
            .AddPath(tempFile.Path)
            .WithAffectedFiles([..affectedPaths])
            .WithTempFile(tempFile);
    }
}