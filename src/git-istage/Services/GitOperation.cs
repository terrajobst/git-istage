using System.Collections.Immutable;
using System.Text;

namespace GitIStage.Services;

internal sealed partial class GitOperation
{
    private GitOperation(string command)
        : this(command, null, [], null)
    {
    }

    private GitOperation(string command,
        TempFile? tempFile,
        ImmutableArray<string> affectedFiles,
        GitOperationResult? result)
    {
        Command = command;
        TempFile = tempFile;
        AffectedFiles = affectedFiles;
        Result = result;
    }

    public string Command { get; }

    public TempFile? TempFile { get; }

    public ImmutableArray<string> AffectedFiles { get; }

    public GitOperationResult? Result { get; }

    private GitOperation WithCommand(string value)
    {
        return new GitOperation(value, TempFile, AffectedFiles, Result);
    }

    private GitOperation WithTempFile(TempFile? tempFile)
    {
        return new GitOperation(Command, tempFile, AffectedFiles, Result);
    }

    private GitOperation WithAffectedFiles(ImmutableArray<string> value)
    {
        return new GitOperation(Command, TempFile, value, Result);
    }

    public GitOperation WithResult(GitOperationResult? value)
    {
        return new GitOperation(Command, TempFile, AffectedFiles, value);
    }

    private GitOperation AddOption(string option)
    {
        return WithCommand(Command + " " + option);
    }

    private GitOperation AddOptionIf(string option, bool condition)
    {
        return condition ? AddOption(option) : this;
    }

    private GitOperation AddPath(string path)
    {
        var needsQuoting = !path.StartsWith('\"') && path.Contains(' ');
        if (!needsQuoting)
            return AddOption(path);

        var sb = new StringBuilder(Command);
        sb.Append(' ');
        sb.Append('"');

        foreach (var c in path)
        {
            if (c == '"')
                sb.Append('"');

            sb.Append(c);
        }

        sb.Append('"');

        return WithCommand(sb.ToString());
    }

    private GitOperation AddAffectedFile(string path)
    {
        return WithAffectedFiles(AffectedFiles.Add(path));
    }
}