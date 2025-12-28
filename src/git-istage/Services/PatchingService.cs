using GitIStage.Patches;
using GitIStage.Patching;
using GitIStage.UI;

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
            var change = fileDocument.GetEntry(selectedLine);
            if (change is not null)
            {
                var canBeHandled = change.ChangeKind is PatchEntryChangeKind.Added or
                                                        PatchEntryChangeKind.Renamed or
                                                        PatchEntryChangeKind.Modified or
                                                        PatchEntryChangeKind.Deleted;

                if (canBeHandled)
                {
                    if (direction == PatchDirection.Stage)
                    {
                        var path = change.ChangeKind == PatchEntryChangeKind.Deleted
                                    ? change.OldPath
                                    : change.NewPath;
                        _gitService.ExecuteGit($"add \"{path}\"");
                    }
                    else if (direction == PatchDirection.Unstage)
                    {
                        if (change.ChangeKind == PatchEntryChangeKind.Deleted)
                            _gitService.ExecuteGit($"restore --staged \"{change.OldPath}\"");
                        else
                            _gitService.ExecuteGit($"reset \"{change.NewPath}\"");
                    }
                    else if (direction == PatchDirection.Reset)
                    {
                        if (change.ChangeKind == PatchEntryChangeKind.Added)
                        {
                            _gitService.ExecuteGit($"add \"{change.NewPath}\"");
                            _gitService.ExecuteGit($"rm -f \"{change.NewPath}\"");
                        }
                        else
                            _gitService.ExecuteGit($"checkout \"{change.OldPath}\"");
                    }
                }
            }
        }
        else
        {
            var document = (PatchDocument)_documentService.Document;
            var line = document.Patch.Lines[selectedLine];
            if (!line.Kind.IsAddedOrDeletedLine())
                return;

            IEnumerable<int> lines;
            if (!entireHunk)
            {
                lines = new[] { selectedLine };
            }
            else
            {
                var start = document.Patch.FindStartOfChangeBlock(selectedLine);
                var end = document.Patch.FindEndOfChangeBlock(selectedLine);
                var length = end - start + 1;
                lines = Enumerable.Range(start, length);
            }

            var patch = document.Patch.SelectForApplication(lines, direction);
            _gitService.ApplyPatch(patch, direction);
        }
    }
}