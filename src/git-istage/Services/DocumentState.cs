using System.Collections.Frozen;
using GitIStage.Patches;
using GitIStage.Text;
using GitIStage.UI;
using IGrammar = TextMateSharp.Grammars.IGrammar;
using GitIStagePatch = GitIStage.Patches.Patch;

namespace GitIStage.Services;

internal sealed class DocumentState
{
    private static readonly FrozenDictionary<PatchEntry, LineHighlights> NoHighlights = FrozenDictionary<PatchEntry, LineHighlights>.Empty;

    private static readonly PatchDocument EmptyPatchDocument = new(GitIStagePatch.Empty, NoHighlights);

    public static DocumentState Empty { get; } = new(EmptyPatchDocument, EmptyPatchDocument);

    private DocumentState(PatchDocument workingCopyDocument, PatchDocument stageDocument)
    {
        WorkingCopyDocument = workingCopyDocument;
        StageDocument = stageDocument;
    }

    public bool HasHighlighting => WorkingCopyDocument.Highlights != NoHighlights ||
                                   StageDocument.Highlights != NoHighlights;

    public PatchDocument WorkingCopyDocument { get; }

    public PatchDocument StageDocument { get; }

    public bool ShouldPerformIncrementalUpdate(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        bool withHighlights)
    {
        if (!withHighlights || withHighlights && !HasHighlighting)
            return false;

        const int maxNumberOfChangesForIncrementalIUpdate = 3;
        var workingCopyChanges = GetCountOfChangedEntries(WorkingCopyDocument.Patch, workingCopyPatch);
        var stageChanges = GetCountOfChangedEntries(StageDocument.Patch, stagePatch);

        return workingCopyChanges <= maxNumberOfChangesForIncrementalIUpdate &&
               stageChanges <= maxNumberOfChangesForIncrementalIUpdate;

        static int GetCountOfChangedEntries(GitIStagePatch oldPatch, GitIStagePatch newPatch)
        {
            var oldEntries = PatchEntryKey.GetSet(oldPatch);
            return newPatch.Entries.Count(e => !oldEntries.Contains(e));
        }
    }

    public static DocumentState Create(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch)
    {
        return Create(null, null, workingCopyPatch, stagePatch, null);
    }

    public static DocumentState CreateHighlighted(
        string workingDirectory,
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch)
    {
        return Create(null, null, workingCopyPatch, stagePatch, workingDirectory);
    }

    public DocumentState IncrementalUpdate(
        string workingDirectory,
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch)
    {
        return Create(WorkingCopyDocument, StageDocument, workingCopyPatch, stagePatch, workingDirectory);
    }

    public DocumentState DropHighlights()
    {
        var workingCopyDocument = new PatchDocument(WorkingCopyDocument.Patch, NoHighlights);
        var stageDocument = new PatchDocument(StageDocument.Patch, NoHighlights);
        return new DocumentState(workingCopyDocument, stageDocument);
    }

    private static DocumentState Create(
        PatchDocument? oldWorkingCopyDocument,
        PatchDocument? oldStageDocument,
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        string? workingDirectory)
    {
        if (workingDirectory is null)
        {
            var workingCopyWithoutHighlights = new PatchDocument(workingCopyPatch, NoHighlights);
            var stageWithoutHighlights = new PatchDocument(stagePatch, NoHighlights);
            return new DocumentState(workingCopyWithoutHighlights, stageWithoutHighlights);
        }

        var workingCopyEntries = workingCopyPatch.Entries.ToDictionary(e => e.NewPath);
        var stageEntries = stagePatch.Entries.ToDictionary(e => e.NewPath);
        var files = new SortedSet<string>(workingCopyEntries.Keys.Concat(stageEntries.Keys));

        var workingCopyHighlightsByEntry = new Dictionary<PatchEntry, LineHighlights>();
        AddUnchangedEntries(workingCopyHighlightsByEntry, oldWorkingCopyDocument, workingCopyPatch);

        var stageHighlightsByEntry = new Dictionary<PatchEntry, LineHighlights>();
        AddUnchangedEntries(stageHighlightsByEntry, oldStageDocument, stagePatch);

        foreach (var file in files)
        {
            var workingCopyEntry = workingCopyEntries.GetValueOrDefault(file);
            var stageEntry = stageEntries.GetValueOrDefault(file);

            var workingCopyNeedsHighlighting =
                workingCopyEntry is not null &&
                !workingCopyHighlightsByEntry.ContainsKey(workingCopyEntry);

            var stageNeedsHighlighting =
                stageEntry is not null &&
                !stageHighlightsByEntry.ContainsKey(stageEntry);

            if (!workingCopyNeedsHighlighting && !stageNeedsHighlighting)
                continue;

            var fullPath = Path.Combine(workingDirectory!, file);
            var workingCopyLines = TextLines.FromFile(fullPath);
            if (workingCopyLines.Count == 0)
                continue;

            var stageLines = workingCopyLines.ApplyReversed(workingCopyEntry);
            var committedLines = stageLines.ApplyReversed(stageEntry);

            var grammar = SyntaxTokenizer.Instance.GetGrammar(file);
            AddHighlightsForEntry(grammar, workingCopyHighlightsByEntry, workingCopyEntry, stageLines);
            AddHighlightsForEntry(grammar, stageHighlightsByEntry, stageEntry, committedLines);
        }

        var workingCopyDocument = new PatchDocument(workingCopyPatch, workingCopyHighlightsByEntry.ToFrozenDictionary());
        var stageDocument = new PatchDocument(stagePatch, stageHighlightsByEntry.ToFrozenDictionary());
        return new DocumentState(workingCopyDocument, stageDocument);

        static void AddHighlightsForEntry(
            IGrammar? grammar,
            Dictionary<PatchEntry, LineHighlights> receiver,
            PatchEntry? patchEntry,
            TextLines original)
        {
            if (patchEntry is null || grammar is null)
                return;

            var originalWithStates = TextLinesWithStates.Create(original, grammar);
            var lineHighlights = LineHighlights.ForPatchEntry(originalWithStates, patchEntry, grammar);
            receiver.Add(patchEntry, lineHighlights);
        }
    }

    private static void AddUnchangedEntries(Dictionary<PatchEntry, LineHighlights> highlightsByEntry, PatchDocument? oldDocument, GitIStagePatch patch)
    {
        if (oldDocument is null || oldDocument.Highlights == NoHighlights)
            return;

        var newEntries = PatchEntryKey.GetSet(patch);
        var unchangedEntries = oldDocument.Patch.Entries.Where(e => newEntries.Contains(e));

        foreach (var unchangedEntry in unchangedEntries)
        {
            if (oldDocument.Highlights.TryGetValue(unchangedEntry, out var oldHighlights))
                highlightsByEntry.Add(unchangedEntry, oldHighlights);
        }
    }
}
