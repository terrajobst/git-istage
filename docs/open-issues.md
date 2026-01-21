# Open Issues

This document tracks open issues I'd like to address.

## Single file mode

We have way to put any file at the top of view but we don't have a way to only
render a single entry.

## Syntax colorization

We have a branch with a spike that shows how this can be done. This reveals a
few challenges:

1. We need to make it incremental and/or quicker. Otherwise, every time we stage
   a line it flickers because it first goes to no highlighting and then the
   highlighting comes back. That's bad.
    - We currently do the first the update on a worker thread and all subsequent
      updates on the UI thread. That works to reduce flicker because the
      incremental updates are fast enough. Flicker occurs when we do the
      incremental updates on a background thread because until we have the new
      state we can't highlight. Unless we find a way to reuse old highlighting
      data until we have accurate data, doing incremental updates on the UI
      thread this way is probably the best approach.
    - Each patch entry needs an `OriginalSourceText` and `OriginalLineStates`
    - They are different for working copy and stage:
        - The original for working copy is the staged version
        - The original for staged is the committed version
    - They can both be retrieved from Git or computed.
        - To compute the staged version, we reverse apply the working copy patch
          to the working copy.
        - To compute the committed version, we reverse apply the staged patch to
          the staged version.
    - We can independently update entries when we update the patch. Most of this
      can be omitted when we don't want to perform syntax highlighting.
    - Doing it a the entry level (rather than at the patch level) will help
      greatly for large patches (i.e. when many files are changed). We could
      even reduce the update cost by storing patches that only contain a single
      file and then have the `PatchDocument` multiplex over a set of patches. In
      that mode, both the patch as well as the highlighting data would be
      updatable without having to update anything else. That feels desirable.
    - However, we are violating TextMateSharp threading rules when we have
      multiple documents because they will concurrently call into the singleton
      `SyntaxTheme` to get a grammar. We solved with a single threaded task
      scheduler where we can ensure that multiple background operations for
      highlighting are performed on a single thread. This fells unnecessary
      complicated. It seems easier to change the flow to handle this in the
      document service such that `PatchDocument` remains immutable. When we
      compute the initial `PatchDocument`, we can split this into two parts:
      First we create the patch document, with no syntax highlighting. On a
      worker thread we compute highlighting data for each document. Once a
      document is completed, it's updated and the UI thread raises the document
      changed event, just like we would for other changes. This way,
      highlighting is performed on a single thread.
    - We should allow turning syntax highlighting on/off. This should prevent
      any expensive operations for computing it. This should be incremental as
      well, as in, it shouldn't force to recompute the patch, just syntax
      information.

2. We need to allow documents to change. Maybe not their contents, but for sure
   their highlighting information. Right now, we hacked it by having a static
   `Application` instance that we can force to re-render. Not great.

3. There seems to be a bug somewhere that causes a hang in
   `PatchDocument.LoadStyles`.

### Remaining work
 
* Clean-up
    - Move types separate files and have it make sense
    - Consider renaming some of the classes
    - Review and remove unused additions
    - Add tests for the highlighter
    - Remove items from this document when you're done
* Simplify the patch document's APIs for getting styles. All callers should be
  passing in a list to collect the styles they care about.
* Extract other colors from TextMate's theme
    - In fact, consider making the theme customizable and use it for the rest of
      the app.

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
