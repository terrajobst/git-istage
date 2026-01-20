using System.Diagnostics;
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

    public void ApplyPatch(Document document, PatchDirection direction, bool entireHunk, int startLine, int lineCount = 0)
    {
        PatchDocument? patchDocument = document as PatchDocument;
        FileDocument? fileDocument = document as FileDocument;
        
        var indices = Enumerable.Range(startLine, lineCount + 1);

        if (fileDocument is not null)
        {
            using (_gitService.SuspendEvents())
            {
                var changes = indices.Select(fileDocument.GetEntry).Where(e => e is not null).Select(e => e!);
                foreach (var change in changes)
                {
                    var canBeHandled = change.Change is PatchEntryChange.Added or
                                                        PatchEntryChange.Renamed or
                                                        PatchEntryChange.Modified or
                                                        PatchEntryChange.Deleted;

                    if (canBeHandled)
                    {
                        if (direction == PatchDirection.Stage)
                        {
                            var path = change.Change == PatchEntryChange.Deleted
                                ? change.OldPath
                                : change.NewPath;
                            _gitService.Add(path);
                        }
                        else if (direction == PatchDirection.Unstage)
                        {
                            if (change.Change == PatchEntryChange.Deleted)
                                _gitService.RestoreStaged(change.OldPath);
                            else
                                _gitService.Reset(change.NewPath);
                        }
                        else if (direction == PatchDirection.Reset)
                        {
                            if (change.Change == PatchEntryChange.Added)
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
        }
        else if (patchDocument is not null)
        {
            var lines = entireHunk
                ? indices.Select(i => patchDocument.Patch.Lines[i].AncestorsAndSelf().OfType<PatchHunk>().FirstOrDefault())
                         .Where(h => h is not null)
                         .Select(h => h!)
                         .SelectMany(h => h.Lines)
                         .Select(l => l.TextLine.LineIndex)
                : indices.Where(i => patchDocument.Patch.Lines[i].Kind.IsAddedOrDeletedLine());

            var linesMaterialized = lines.ToArray();
            
            if (linesMaterialized.Length > 0)
            {
                var patch = patchDocument.Patch.SelectForApplication(linesMaterialized, direction);
                _gitService.ApplyPatch(patch, direction);
            }
        }
        else
        {
            throw new UnreachableException($"Unexpected document {document}");
        }
    }
}