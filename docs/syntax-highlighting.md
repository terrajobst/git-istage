# Syntax Highlighting

We currently do the first the update on a worker thread and all subsequent
updates on the UI thread. That works to reduce flicker because the incremental
updates are fast enough. Flicker occurs when we do the incremental updates on a
background thread because until we have the new state we can't highlight. Unless
we find a way to reuse old highlighting data until we have accurate data, doing
incremental updates on the UI thread this way is probably the best approach.

For each patch entry we need to compute the staged contents and the committed
contents:

* To compute the staged version, we reverse apply the working copy patch
  to the working copy.
* To compute the committed version, we reverse apply the staged patch to the
  staged version.

We can independently update entries when we update the patch. Most of this can
be omitted when we don't want to perform syntax highlighting.

Doing it a the entry level (rather than at the patch level) helps greatly for
large patches (i.e. when many files are changed).
