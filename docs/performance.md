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
state and that's unfortunately more and more expensive.

It seems the only sensible alternative is to make `git istage` understand
patches such that we can compute the new patch when staging a line without the
need to ask Git for it. This would mean that the cost of staging is reduced to
computing the line/hunk based patch and applying it, which tends to be fast
enough. We could optimize this further by not blocking the UI on the application
of the patch, which we don't need assuming we can compute the new patch. At this
point, the UI should always feel snappy, even in larger repos.

Ideally, we'd eliminate the need for LibGit2Sharp:

* We only use it to compute the patch, but since it can't apply any patches, we
  need to shell out to Git for that anyway. We also shell out for committing and
  shelving.
* You'd think we could use use `git diff` and `git diff --cached` but
  unfortunately Git doesn't support `git diff --include-untracked` so we don't
  get a patch for untracked files, which we want for our workflow.
* We could probably use `git status --untracked-files=all` and simply create an
  artificial patch.