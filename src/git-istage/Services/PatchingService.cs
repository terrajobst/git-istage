using GitIStage.Patches;
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
                        _gitService.Add(path);
                    }
                    else if (direction == PatchDirection.Unstage)
                    {
                        if (change.ChangeKind == PatchEntryChangeKind.Deleted)
                            _gitService.RestoreStaged(change.OldPath);
                        else
                            _gitService.Reset(change.NewPath);
                    }
                    else if (direction == PatchDirection.Reset)
                    {
                        if (change.ChangeKind == PatchEntryChangeKind.Added)
                        {
                            _gitService.Add(change.NewPath);
                            _gitService.RemoveForce(change.NewPath);
                        }
                        else
                            _gitService.Checkout(change.OldPath);
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
                var hunk = document.Patch.Lines[selectedLine].AncestorsAndSelf().OfType<PatchHunk>().First();
                var startLine = hunk.Lines.First().TextLine.LineIndex;
                var lineCount = hunk.Lines.Length;
                lines = Enumerable.Range(startLine, lineCount);
            }

            var patch = document.Patch.SelectForApplication(lines, direction);
            _gitService.ApplyPatch(patch, direction);
        }
    }
}