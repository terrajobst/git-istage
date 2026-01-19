# Handling of `\ No newline at end of file`

If we have a file that doesn't have a final line break and we modify the last
line we get a diff like this:

```diff
-Last Line
\ No newline at end of file
+New Last Line
\ No newline at end of file
```

Or this, in case the new last line has a line break:

```diff
-Last Line
\ No newline at end of file
+New Last Line
```

If we're now staging only the addition, we currently produce the following patch:

```diff
 Last Line
\ No newline at end of file
+New Last Line
\ No newline at end of file
```

That's because our algorithm is quite simple: when we produce a partial patch we
simply ignore all hunks that we didn't select lines from and modify the other
hunks by flipping all modifications of unselected lines into context lines.

But that's wrong because when we apply this patch it produces this outcome:

```diff
-Last Line
\ No newline at end of file
+Last LineNew Last Line
\ No newline at end of file
```

IOW, we append to the existing last line. The correct patch would be this:

```diff
-Last Line
\ No newline at end of file
+Last Line
+New Last Line
\ No newline at end of file
```

The rule is:

If we stage any lines after `\ No newline at end of file` we must

1. Include it and the line before it.
2. Repeat the line before it with an addition marker