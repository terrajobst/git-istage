# Open Issues

This document tracks open issues I'd like to address.

## Single file mode

We have way to put any file at the top of view but we don't have a way to only
render a single entry.

## Syntax colorization

We should use a syntax highlighter (probably `TextMateSharp`) to colorize the
code.

We have a branch with a spike that shows how this can be done. This reveals two
challenges:

1. We need to make it incremental and/or quicker. Otherwise, every time we stage
   a line it flickers because it first goes to no highlighting and then the
   highlighting comes back. That's bad.

2. We need to allow documents to change. Maybe not their contents, but for sure
   their highlighting information. Right now, we hacked it by having a static
   `Application` instance that we can force to re-render. Not great.

### Incremental updates

To support syntax highlighting we need to have the file in three versions:

1. **Working copy**. We compute this by just reading the file from disk.
2. **Committed**. We compute this from (1) by applying the working copy patch in
   reverse mode.
3. **Staged**. We compute this from (1) by applying the stage patch.

It might be best if we had a `SourceText` representation that can model changes.
So when I say 'apply the patch' we might not actually construct a `string` that
contains the entire changed document. Maybe we compute a `SourceText` + changes.
Or maybe we make `SourceText` actually abstract and have a derived version like
`ChangedSourceText`, similar to how Roslyn does it. We might be able to keep it
simpler as we only have to handle added/removed lines.

TextMateSharp has state between lines, represented via `IStateStack`. They seem
to be immutable. When doing highlighting, we can keep this around, i.e. one per
line.

We also should be re-thinking when we need to update the source documents. Right
now, we don't have to worry about that as we only have to refresh patches:

| Operation   | Working Copy Patch | Staged Patch |
| ----------- | ------------------ | ------------ |
| **Stage**   | Yes                | Yes          |
| **Unstage** | Yes                | Yes          |
| **Reset**   | Yes                | No           |
| **Stash**   | Yes                | No           |
| **Commit**  | Yes                | Yes          |

| Operation   | Working Copy | Staged | Committed |
| ----------- | ------------ | ------ | --------- |
| **Stage**   | No           | Yes    | No        |
| **Unstage** | No           | Yes    | No        |
| **Reset**   | Yes          | No     | No        |
| **Stash**   | Yes          | No     | No        |
| **Commit**  | Yes          | Yes    | Yes       |

Here is how we'll highlight the Working Copy patch:

1. Take the working copy file, highlight each line and remember the
   `IStateStack`.
2. **Working Copy - New**. That's the result from (1).
3. **Working Copy - Old**. Apply the patch in reverse. Don't create a result
   document, just keep track of what the current line is and whether it's from
   the patch or (1). We'll reuse highlighting results from (1). Before we can
   reuse a line, we need to compare the previous line state stack between what
   we have and what came from (1). If they differ, we need to re-highlight the
   line.

Here is how we'll highlight the Staged patch:

1. Take (3) as the input.
2. **Staged - Old**. That's the result of (1)
3. **Staged - New**. Same algorithm as in step (3) above, the only difference is
   that we apply the patch without reversing it.

We can keep the highlighted working copy document around because when we just
stage / unstage we don't have to update it as the working copy doesn't change,
only the patches do.

### Text Representation

We can do one of two things here: we can either have representations for the
actual snapshots that we can query when highlighting the patch or we can come up
with a way we can walk the lines incrementally without having to construct full
documents.

When walking the patch, we'd have two instances, one for old and one for new.
Depending on what line we're on, we advance one of them or both of them. At any
point we can ask for the highlights of the current line.

```C#
public sealed class CommittedHighlights
{
    private readonly IGrammar _grammar;
    private readonly ImmutableArray<IStateStack> _lineStates { get; }
    private readonly ImmutableArray<ImmutableArray<StyledSpan>> _lineHighlights { get; }

    private CommittedHighlights(IGrammar grammar,
                                ImmutableArray<IStateStack> lineStates,
                                ImmutableArray<ImmutableArray<StyledSpan>> lineHighlights)
    {
        _grammar = grammar;
        _lineStates = lineStates;
        _lineHighlights = lineHighlights;
    }

    public CommittedHighlights Create(IGrammar grammar,
                                      SourceText workingCopyContents,
                                      PatchEntry? workingCopyPatch,
                                      PatchEntry? stagedPatch)
    {
        var lineStateBuilder = ImmutableArray.CreateBuilder<IStateStack>();
        var lineHighlightBuilder = ImmutableArray.CreateBuilder<ImmutableArray<StyledSpan>>();

        // Select the patch to use
        PatchEntry patch = stagedPatch ?? workingCopyPatch;

        // We apply the patch in reverse to workingCopyContents. This gives us
        // the committed state.
        var workingCopyLine = 0;
        IStateStack? oldState = null;

        foreach (var hunk in patch.Hunks)
        {
            var oldLine = hunk.OldRange.Line - 1;
            var newLine = hunk.NewRange.Line - 1;
            HighlightRange(workingCopyContents, workingCopyLine, newLine);
            workingCopyLine = newLine;

            foreach (var line in hunk.Lines)
            {
                var text = line.Text.Slice(1);

                if (line.Kind == Context)
                {
                    // Highlight
                    (var highlights, oldState) = Highlight(text, oldState);
                    AddHighlights(receiver, line, highlights);
                    lineStateBuilder.Add(state);
                    oldLine++;
                    newLine++;
                }
                else if (line.Kind == Addition)
                {
                    // Ignore
                    newLine++;
                }
                else if (line.Kind == Removal)
                {
                    // Highlight                  
                    (var highlights, oldState) = Highlight(text, oldState);
                    AddHighlights(receiver, line, highlights);
                    lineStateBuilder.Add(state);
                    oldLine++;
                }
            }
        }

        HighlightRange(workingCopyContents, workingCopyLine, workingCopyContents.Lines.Length);

        return new CommittedHighlights(grammar, lineStateBuilder.ToImmutable(), lineHighlightsBuilder.ToImmutable());

        static IStateStack HighlightRange(SourceText text, int startLine, int endLine, IStateStack? state)
        {
            for (var i = startLine; i < endLine; i++)
            {
                var line = text.Lines[i];
                var lineText = line.Text;
                (var highlights, state) = Highlight(text, state);
                lineStateBuilder.Add(state);
            }

            return state;
        }
    }

    public void GetHighlights(List<StyledSpan> receiver, PatchEntry patch)
    {
        IStateStack? newState = null;

        foreach (var hunk in patch.Hunks)
        {
            var oldLine = hunk.OldRange.Line - 1;
            var newLine = hunk.NewRange.Line - 1;

            if (newState is null)
                newState = _lineStates[oldLine];

            foreach (var line in hunk.Lines)
            {
                var line = hunk[hunkLine];
                var text = line.Text.Slice(1);
                if (line.Kind == Context)
                {
                    (var highlights, newState) = Highlight(text, newState);
                    AddHighlights(receiver, line, highlights);
                    oldLine++;
                    newLine++;
                }
                else if (line.Kind == Addition)
                {
                    (var highlights, newState) = Highlight(text, newState);
                    AddHighlights(receiver, line, highlights);
                    newLine++;
                }
                else if (line.Kind == Removal)
                {
                    var highlights = _lineHighlights[oldLine];
                    AddHighlights(receiver, line, highlights);
                    oldLine++;
                }
            }
        }
    }

    private (IEnumerable<StyledSpan> Highlights, IStateStack State) Highlight(ReadOnlySpan<char> text, IStateStack? state)
    {
        // TODO
    }

    private static void AddHighlights(List<StyledSpan> receiver, PatchLine line, IEnumerable<StyledSpan> spans)
    {
        // TODO: Translate line-relative to absolute
    } 
}
```

## Theming

It would be nice to support theming.

## LibGit2Sharp

Ideally, we'd eliminate the need for LibGit2Sharp:

* We only use it to compute the patch, but since it can't apply any patches, we
  need to shell out to Git for that anyway. We also shell out for committing and
  shelving.
* You'd think we could use `git diff` and `git diff --cached` but unfortunately
  Git doesn't support `git diff --include-untracked` so we don't get a patch for
  untracked files, which we want for our workflow.
* We could probably use `git status --untracked-files=all` and simply create an
  artificial patch.

We can emulate the intended behavior for comparing our working copy against HEAD
by using a script that appends a diff between untracked files and `/dev/null` to
the output of `git diff`:

```PowerShell
# Windows using PowerShell
git diff --find-copies-harder ; `
git ls-files --others --exclude-standard | `
ForEach-Object { git diff --no-index /dev/null $_ }
```

```Bash
# Bash for everyone else
git diff --find-copies-harder ; \
git ls-files --others --exclude-standard | \
xargs -L 1 git diff --no-index /dev/null
```

## Conflicts

Running `git diff` while conflicts are active looks different. Our patch parser
can't handle those. If we simply take in the output from `git diff`, we'll need
to handle them in parsing.

```diff
diff --cc lorem.txt
index 14eb246,98d39d3..0000000
--- a/lorem.txt
+++ b/lorem.txt
@@@ -2,7 -2,7 +2,11 @@@ Additio

  # Lorem

++<<<<<<< HEAD
 +Change in Main
++=======
+ Change in Test
++>>>>>>> de5c834 (Change in branch Test)

  Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
  aliquip ex ea commodo consequat.
```

The format is described as [combined diff format].

I think for now we should just preprocess this into a form like this:

```diff
diff --git a/another.txt b/another.txt
!needs merge
```

This provides the information in the UI but doesn't let us do anything with it.

[combined diff format]: https://git-scm.com/docs/diff-format#_combined_diff_format
