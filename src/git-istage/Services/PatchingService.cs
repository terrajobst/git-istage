using GitIStage.Patches;
using GitIStage.UI;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class PatchingService
{
    private readonly GitService _gitService;
    private readonly DocumentService _documentService;

    public PatchingService(GitService gitService, DocumentService documentService)
    {
        _gitService = gitService;
        _documentService = documentService;
    }

    public void ApplyPatch(PatchDirection direction, bool entireHunk, int selectedLine)
    {
        if (_documentService.ViewFiles)
        {
            var fileDocument = (FileDocument)_documentService.Document;
            var change = fileDocument.GetChange(selectedLine);
            if (change is not null)
            {
                var canBeHandled = change.Status is ChangeKind.Added or
                    ChangeKind.Renamed or
                    ChangeKind.Modified or
                    ChangeKind.Deleted;

                if (canBeHandled)
                {
                    if (direction == PatchDirection.Stage)
                        _gitService.ExecuteGit($"add \"{change.Path}\"");
                    else if (direction == PatchDirection.Unstage)
                        _gitService.ExecuteGit($"reset \"{change.Path}\"");
                    else if (direction == PatchDirection.Reset)
                        _gitService.ExecuteGit($"checkout \"{change.Path}\"");
                }
            }
        }
        else
        {
            var document = (PatchDocument)_documentService.Document;
            var line = document.Lines[selectedLine];
            if (!line.Kind.IsAdditionOrRemoval())
                return;

            IEnumerable<int> lines;
            if (!entireHunk)
            {
                lines = new[] { selectedLine };
            }
            else
            {
                var start = document.FindStartOfChangeBlock(selectedLine);
                var end = document.FindEndOfChangeBlock(selectedLine);
                var length = end - start + 1;
                lines = Enumerable.Range(start, length);
            }

            var patch = Patching.ComputePatch(document, lines, direction);
            _gitService.ApplyPatch(patch, direction);
        }
    }
}