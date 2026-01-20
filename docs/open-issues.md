# Open Issues

This document tracks open issues I'd like to address.

## Go To File

When in file mode, hitting enter should switch to patch mode and put the top
line as the start of that file.

## Document Views

We have quite a few different documents here:

* Working Copy Patch
* Working Copy Files
* Stage Patch
* Stage Files
* Help
* Last Error

We should elevate this to a fist class concept and allow switching between them,
restoring scroll position and selection.

## Git Command Log

It would be useful to have a history of Git commands, just like LazyGit does.
Should include any errors. We should replace the error document with the log
document, with new entries being prepended.

## Background loading

We should use a file system watcher and automatically reload when files change
on disk.

## Syntax colorization

We should use a syntax highlighter (probably `TextMateSharp`) to colorize the
code.

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
