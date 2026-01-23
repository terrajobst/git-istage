using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.UI;

namespace GitIStage.Services;

internal sealed class PatchingService
{
    private readonly GitService _gitService;
    private readonly FileWatchingService? _fileWatchingService;

    public PatchingService(GitService gitService, FileWatchingService? fileWatchingService)
    {
        _gitService = gitService;
        _fileWatchingService = fileWatchingService;
    }

    public void ApplyPatch(Document document, PatchDirection direction, bool entireHunk, int startLine, int lineCount = 0)
    {
        var indices = Enumerable.Range(startLine, lineCount + 1);
        ApplyPatch(document, direction, entireHunk, indices);
    }

    private void ApplyPatch(Document document, PatchDirection direction, bool entireHunk, IEnumerable<int> indices)
    {
        IEnumerable<GitOperation> operations;

        if (document is FileDocument fileDocument)
        {
            operations = ApplyPatch(fileDocument, direction, indices);
        }
        else if (document is PatchDocument patchDocument)
        {
            operations = ApplyPatch(patchDocument, direction, entireHunk, indices);
        }
        else
        {
            throw new UnreachableException($"Unexpected document {document}");
        }

        using (_fileWatchingService?.SuspendEvents())
            _gitService.Execute(operations);
    }

    private static IEnumerable<GitOperation> ApplyPatch(FileDocument fileDocument, PatchDirection direction, IEnumerable<int> indices)
    {
        var changes = indices.Select(fileDocument.GetEntry)
                             .Where(e => e is not null)
                             .Select(e => e!);

        foreach (var change in changes)
        {
            var canBeHandled = change.Change is PatchEntryChange.Added
                                             or PatchEntryChange.Renamed
                                             or PatchEntryChange.Modified
                                             or PatchEntryChange.Deleted;

            if (!canBeHandled)
                continue;

            switch (direction)
            {
                case PatchDirection.Stage:
                    yield return GitOperation.Add(change.NewPath);
                    break;
                case PatchDirection.Unstage when change.Change == PatchEntryChange.Deleted:
                    yield return GitOperation.Restore(staged: true, change.OldPath);
                    break;
                case PatchDirection.Unstage:
                    yield return GitOperation.Reset(change.OldPath);
                    break;
                case PatchDirection.Reset when change.Change == PatchEntryChange.Added:
                    yield return GitOperation.Add(change.NewPath);
                    yield return GitOperation.Remove(change.NewPath, force: true);
                    break;
                case PatchDirection.Reset:
                    yield return GitOperation.Checkout(change.OldPath);
                    break;
            }
        }
    }

    private static IEnumerable<GitOperation> ApplyPatch(PatchDocument patchDocument, PatchDirection direction, bool entireHunk, IEnumerable<int> indices)
    {
        var lines = entireHunk
            ? indices.Select(i => patchDocument.Patch.Lines[i].AncestorsAndSelf().OfType<PatchHunk>().FirstOrDefault())
                     .Where(h => h is not null)
                     .Select(h => h!)
                     .SelectMany(h => h.Lines)
                     .Select(l => l.LineIndex)
            : indices.Where(i => patchDocument.Patch.Lines[i].Kind.IsAddedOrDeletedLine());

        var linesMaterialized = lines.Order().Distinct().ToArray();

        if (linesMaterialized.Length > 0)
        {
            var patch = patchDocument.Patch.SelectForApplication(linesMaterialized, direction);
            var reverse = direction is PatchDirection.Reset or PatchDirection.Unstage;
            var cached = direction != PatchDirection.Reset;
            yield return GitOperation.Apply(patch, reverse, cached, verbose: true);
        }
    }
}