using System.Diagnostics;
using System.Text;
using GitIStage.Patching.Headers;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

// TODO: Make interacting with the Patch DOM more sensible.
//
// I think we want each node to know its Patch. We can then lazily construct
// a dictionary from node -> parent.
//
// Which allows us to add Ancestors(), Descendents(), and Descendents(TextSpan).
//
// Now we can ask things like:
//
//      var selectedHunkLines = patch.Descendents(selectedRange).OfType<PatchHunkLine>()
//                                   .Select(l => (Entry: l.Ancestors.OfType<PatchEntry>().Single(),
//                                                 Hunk: l.Ancestors.OfType<PatchHunk>().Single(),
//                                                 Line: l))
//
// all the selected hunk lines.

public static class PatchApplication
{
    public static Patch StageLines(this Patch patch, int startLineIndex, int count)
    {
        ThrowIfNull(patch);
        ThrowIfLessThan(startLineIndex, 0);
        ThrowIfGreaterThan(startLineIndex, patch.Text.Lines.Length);
        ThrowIfGreaterThan(count, patch.Text.Lines.Length - startLineIndex);

        var spanStart = patch.Text.Lines[startLineIndex].Span.Start;
        var spanEnd = patch.Text.Lines[startLineIndex + count - 1].Span.End;
        var span = TextSpan.FromBounds(spanStart, spanEnd);

        var source = patch.ToString().AsSpan();
        var patchBuilder = new StringBuilder();

        foreach (var entry in patch.Entries)
        {
            if (!entry.Span.OverlapsWith(span))
                continue;

            var hasAnyHunks = entry.Hunks.Any(h => h.Lines.Where(l => l.Kind is PatchNodeKind.AddedLine or
                                                                                PatchNodeKind.DeletedLine)
                                                          .Any(l => l.Span.OverlapsWith(span)));
            if (!hasAnyHunks)
                continue;

            var allHunksSelected = entry.Hunks.All(h => h.Lines.Where(l => l.Kind is PatchNodeKind.AddedLine or
                                                                                     PatchNodeKind.DeletedLine)
                                                               .All(l => span.Contains(l.Span)));

            foreach (var header in entry.Headers)
            {
                // If we delete the file, the new path header should only be set to /dev/null
                // if we stage the entire file.
                if (!allHunksSelected && header is NewPathPatchEntryHeader { Path: "" })
                {
                    patchBuilder.AppendLine("+++ b/" + entry.OldPath);
                    continue;
                }

                var headerRequiresAllHunks = header.Kind is PatchNodeKind.IndexHeader
                                                         or PatchNodeKind.SimilarityIndexHeader
                                                         or PatchNodeKind.DissimilarityIndexHeader
                                                         or PatchNodeKind.DeletedFileModeHeader;

                if (headerRequiresAllHunks && !allHunksSelected)
                    continue;

                CopyLineToBuilder(source, header.TextLine, patchBuilder);
            }

            var hunkBuilder = new List<MutableHunkLine>();

            foreach (var hunk in entry.Hunks)
            {
                if (!hunk.Span.OverlapsWith(span))
                    continue;

                // Copy hunk

                hunkBuilder.Clear();

                foreach (var line in hunk.Lines)
                {
                    var mutableHunkLine = new MutableHunkLine
                    {
                        Modifier = line.Kind switch
                        {
                            PatchNodeKind.AddedLine => '+',
                            PatchNodeKind.DeletedLine => '-',
                            PatchNodeKind.ContextLine => ' ',
                            PatchNodeKind.NoNewLine => '\\',
                            _ => throw new UnreachableException($"Unexpected hunk line {line.Kind}")
                        },
                        TextLine = line.TextLine
                    };
                    hunkBuilder.Add(mutableHunkLine);
                }

                // Change hunk

                // TODO: Can we always use the start lines?
                //
                // At least we need to keep track of a hunk we didn't include
                // and adjust the next hunk in same entry accordingly.

                var oldStart = hunk.Header.OldLine;
                var newStart = hunk.Header.NewLine;
                var oldCount = 0;
                var newCount = 0;

                foreach (var line in hunkBuilder)
                {
                    var nextLineIndex = patch.Text.GetLineIndex(line.TextLine.Start) + 1;
                    var nextLine = nextLineIndex < patch.Text.Lines.Length
                                    ? patch.Text.Lines[nextLineIndex]
                                    : null;

                    var nextLineIncluded = nextLine is not null &&
                                           nextLine.Span.OverlapsWith(span);

                    var lineIncluded = line.TextLine.Span.OverlapsWith(span) ||
                                       line.Modifier == ' ' ||
                                       (line.Modifier == '\\' && nextLineIncluded);

                    if (!lineIncluded)
                    {
                        if (line.Modifier == '-')
                            line.ModifierAfter = ' ';
                        else
                            line.Exclude = true;
                    }

                    if (!line.Exclude)
                    {
                        if (line.ModifierAfter is ' ' or '+')
                            newCount++;

                        if (line.ModifierAfter is ' ' or '-')
                            oldCount++;
                    }
                }

                if (newStart == 0 && newCount > 0)
                    newStart = 1;

                patchBuilder.Append($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@");
                CopyLineBreakToBuilder(source, hunk.Header.TextLine, patchBuilder);

                foreach (var line in hunkBuilder)
                    CopyMutableLineToBuilder(source, line, patchBuilder);
            }
        }

        return Patch.Parse(patchBuilder.ToString());

        static void CopyLineToBuilder(ReadOnlySpan<char> source, TextLine line, StringBuilder sb)
        {
            CopySpanToBuilder(source, line.SpanIncludingLineBreak, sb);
        }

        static void CopyMutableLineToBuilder(ReadOnlySpan<char> source, MutableHunkLine line, StringBuilder sb)
        {
            if (line.Exclude)
                return;

            var lineSpan = line.TextLine.SpanIncludingLineBreak;
            var lineWithoutModifier = TextSpan.FromBounds(lineSpan.Start + 1, lineSpan.End);

            if (line.TextLine.Length > 1)
                sb.Append(line.ModifierAfter);

            CopySpanToBuilder(source, lineWithoutModifier, sb);
        }

        static void CopyLineBreakToBuilder(ReadOnlySpan<char> source, TextLine line, StringBuilder sb)
        {
            var lineBreakStart = line.Span.End;
            var lineBreakEnd = line.SpanIncludingLineBreak.End;
            var lineBreakSpan = TextSpan.FromBounds(lineBreakStart, lineBreakEnd);
            CopySpanToBuilder(source, lineBreakSpan, sb);
        }

        static void CopySpanToBuilder(ReadOnlySpan<char> source, TextSpan span, StringBuilder sb)
        {
            var start = span.Start;
            var length = span.Length;
            sb.Append(source.Slice(start, length));
        }
    }

    private sealed class MutableHunkLine
    {
        private readonly char _modifierBefore;

        public bool Exclude { get; set; }

        public required char Modifier
        {
            get => _modifierBefore;
            init
            {
                _modifierBefore = value;
                ModifierAfter = value;
            }
        }

        public char ModifierAfter { get; set; }

        public required TextLine TextLine { get; init; }
    }
}