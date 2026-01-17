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
        using (_gitService.SuspendEvents())
        using (_fileWatchingService?.SuspendEvents())
        {
            if (document is FileDocument fileDocument)
            {
                ApplyPatch(fileDocument, direction, indices);
            }
            else if (document is PatchDocument patchDocument)
            {
                ApplyPatch(patchDocument, direction, entireHunk, indices);
            }
            else
            {
                throw new UnreachableException($"Unexpected document {document}");
            }
        }
    }

    private void ApplyPatch(FileDocument fileDocument, PatchDirection direction, IEnumerable<int> indices)
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
                    var path = change.Change == PatchEntryChange.Deleted
                                ? change.OldPath
                                : change.NewPath;
                    _gitService.Add(path);
                    break;
                case PatchDirection.Unstage when change.Change == PatchEntryChange.Deleted:
                    _gitService.RestoreStaged(change.OldPath);
                    break;
                case PatchDirection.Unstage:
                    _gitService.Reset(change.NewPath);
                    break;
                case PatchDirection.Reset when change.Change == PatchEntryChange.Added:
                    _gitService.Add(change.NewPath);
                    _gitService.RemoveForce(change.NewPath);
                    break;
                case PatchDirection.Reset:
                    _gitService.Checkout(change.OldPath);
                    break;
            }
        }
    }

    private void ApplyPatch(PatchDocument patchDocument, PatchDirection direction, bool entireHunk, IEnumerable<int> indices)
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
            _gitService.ApplyPatch(patch, direction);
        }
    }
}