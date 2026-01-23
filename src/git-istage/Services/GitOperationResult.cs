using System.Collections.Immutable;

namespace GitIStage.Services;

internal sealed class GitOperationResult
{
    public GitOperationResult(int exitCode, ImmutableArray<string> output)
    {
        ExitCode = exitCode;
        Output = output;
    }

    public bool Success => ExitCode == 0;

    public int ExitCode { get; }

    public ImmutableArray<string> Output { get; set; }
}