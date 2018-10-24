# What is staging and why would I care?

If you just started with git, you probably have no concept of the *stage* (or as
some people confusingly call it *index*). In other version control systems, the
only thing you can select is which files you want to commit. However, git not
only allows you to do that but also which parts of a file you want to select.

Unfortunately, the built-in command line tooling for that (provided via
`git add -p`) is pretty hard to use and takes way too long.

## Demo

Full a quick demo, check out my video on YouTube:

[![](thumbnail.png)](https://www.youtube.com/watch?v=2nNJly4uim0)

## What git istage does

When you run `git istage` inside your repo, it shows you the set of current
changes, i.e. the output of `git diff`. But in contrast to `git diff`, you can
select individual lines and add them to the stage by hitting <kbd>S</kbd>. You
can also permanently remove changes from your working copy by hitting
<kbd>R</kbd> (be careful: there is no undo for that operation).

At any point you can view the staged changes (i.e. the output of
`git diff --cached`). If you decided that there are certain portions you don't
want to commit, you simply unstage the selected lines by hitting <kbd>U</kbd>.

All three shortcuts can be modified using <kbd>Shift</kbd>. In that case it will
apply to all consecutive changes, called a *block*.

In order to quickly navigated you can use <kbd>←</kbd> and <kbd>→</kbd>, which
will jump between files, and <kbd>[</kbd> and <kbd>]</kbd>, which will jump
from block to block.

You can also customize the way the changes are presented. By using <kbd>+</kbd>
and <kbd>-</kbd> to increase or decrease the number of contextual lines being
displayed. Alternatively, you can use <kbd>\\</kbd> to toggle between viewing
entire files or changes only.

When you are done, simply return to the command line by hitting <kbd>Esc</kbd>
or <kbd>Q</kbd>. Once there, I recommend you run `git stash -u -k`, which will
stash all changes you didn't stage so you can test the code you're about to
commit. Or, you can stash right from inside of git istage by hitting
<kbd>Alt</kbd> <kbd>S</kbd>. Once satisfied, run `git commit` and unstash via
`git stash pop`.

More keyboard shortcuts listed below.

## Keyboard shortcuts

Shortcut | Description
---------|------------
<kbd>Esc</kbd> | Return to command line.
<kbd>Q</kbd> | Return to command line.
<kbd>C</kbd> | Author commit
<kbd>Alt</kbd> <kbd>C</kbd> | Authors commit with `--amend` option
<kbd>Alt</kbd> <kbd>S</kbd> | Stashes changes from the working copy, but leaves the stage as-is.
<kbd>T</kbd> | Toggle between working copy changes and staged changes.
<kbd>R</kbd> | When viewing the working copy, removes the selected line from the working copy.
<kbd>S</kbd> | When viewing the working copy, stages the selected line.
<kbd>U</kbd> | When viewing the stage, unstages the selected line.
<kbd>Shift</kbd> <kbd>R</kbd> | When viewing the working copy, removes the selected block from the working copy.
<kbd>Shift</kbd> <kbd>S</kbd> | When viewing the working copy, stages the selected block.
<kbd>Shift</kbd> <kbd>U</kbd> | When viewing the stage, unstages the selected block.
<kbd>+</kbd> | Increases the number of contextual lines.
<kbd>-</kbd> | Decreases the number of contextual lines.
<kbd>\\</kbd> | Toggles between viewing entire files and changes only.
<kbd>W</kbd> | Toggles between showing and hiding whitespace.
<kbd>↑</kbd> | Selects the previous line.
<kbd>K</kbd> | Selects the previous line.
<kbd>↓</kbd> | Selects the next line.
<kbd>J</kbd> | Selects the next line.
<kbd>←</kbd> | Go to the previous file.
<kbd>→</kbd> | Go to the next file.
<kbd>[</kbd> | Go to previous change block.
<kbd>]</kbd> | Go to next change block.
<kbd>PgUp</kbd> | Selects the line one screen above.
<kbd>PgDown</kbd> | Selects the line one screen below.
<kbd>Space</kbd> | Selects the line one screen below.
<kbd>Home</kbd>| Selects the first line.
<kbd>G</kbd>| Selects the first line.
<kbd>End</kbd> | Selects the last line.
<kbd>Shift</kbd> <kbd>G</kbd> | Selects the last line.
<kbd>Ctrl</kbd> <kbd>PgUp</kbd> | Scrolls up by one screen.
<kbd>Ctrl</kbd> <kbd>PgDown</kbd> | Scrolls down by one screen.
<kbd>Ctrl</kbd> <kbd>←</kbd> | Scrolls left by one character,
<kbd>Ctrl</kbd> <kbd>→</kbd> | Scrolls right by one character,
<kbd>Ctrl</kbd> <kbd>↑</kbd> | Scrolls up by one line.
<kbd>Ctrl</kbd> <kbd>↓</kbd> | Scrolls down by one line.
