# Performance

I've noticed that using `git istage` in larger repos can be painfully slow. The
primary reason is that every time a line is staged, we create a patch, apply it,
and then ask Git to compare the working directory against the index. The latter
is progressively more expensive the larger the repo becomes.

> [!NOTE]
>
> This is mostly an issue when viewing unstaged changes because recomputing the
> diff between the stage and HEAD tends to be fast even in larger repos. In
> other words, unstaging tends to be snappy.

I've tried to optimize the calls to LibGit2Sharp as well Git a bit more but
ultimately we still need to ask Git for a new patch that describes the current
state and that's unfortunately more and more expensive. Options:

* **Incremental Patches**. One option we have is making the patches more
inremental: rather than asking Git for a patch that desribes the entire working
directory, we could just ask for the new patch of the file whose line(s) we
just staged. That should be cheaper as it shouldn't be function of the repo
size but rather of the size of the file.

* **Computing patches ourselves**. Another option is making `git istage`
understand patches such that we can compute the new patch when staging a line
without the need to ask Git for it. This would mean that the cost of staging is
reduced to computing the line/hunk based patch and applying it, which tends to
be fast enough. We could optimize this further by not blocking the UI on the
application of the patch, which we don't need assuming we can compute the new
patch. At this point, the UI should always feel snappy, even in larger repos.

## LibGit2Sharp

Ideally, we'd also eliminate the need for LibGit2Sharp:

* We only use it to compute the patch, but since it can't apply any patches, we
  need to shell out to Git for that anyway. We also shell out for committing and
  shelving.
* You'd think we could use use `git diff` and `git diff --cached` but
  unfortunately Git doesn't support `git diff --include-untracked` so we don't
  get a patch for untracked files, which we want for our workflow.
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

Running `git diff` while conflicts are active looks different:

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
