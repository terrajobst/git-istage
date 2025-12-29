using System.Diagnostics;

namespace GitIStage.Tests;

public class PatchingTests
{
    // TODO: Test exceptions

    [Fact]
    public void Patch_Empty_CanBeParsed()
    {
        var patch = Patch.Parse("");

        patch.Entries.Should().BeEmpty();
        patch.Text.Length.Should().Be(0);

        patch.Should().BeSameAs(Patch.Empty);
    }

    [Fact]
    public void Patch_WithGarbageAtEnd_CannotBeParsed()
    {
        var text = """
                   diff --git a/example.txt b/example.txt
                   index 27ac6d3..7576e3e 100644
                   --- a/path/to/original
                   +++ b/path/to/new
                   @@ -1,0 +1,1 @@
                   +This is an important
                   additional text
                   """;

        Action parse = () => Patch.Parse(text);

        parse.Should()
            .Throw<FormatException>()
            .Which.Message.Should().Be("Invalid patch. Expected 'diff --git' in line 7.");
    }

    [Fact]
    public void Patch_WhitespaceOnly_CannotBeParsed()
    {
        Action parse = () => Patch.Parse("  ");

        parse.Should()
             .Throw<FormatException>()
             .Which.Message.Should().Be("Invalid patch. Expected 'diff --git' in line 1.");
    }

    [Fact]
    public void Patch_InvalidGitDiff_CannotBeParsed()
    {
        Action parse = () => Patch.Parse("git --diff something something");

        parse.Should()
            .Throw<FormatException>()
            .Which.Message.Should().Be("Invalid patch. Expected 'diff --git' in line 1.");
    }

    [Fact]
    public void Patch_Header_DiffGit_CanBeParsed()
    {
        var text = """
                   {:diff --git a/example.txt b/example.txt:}
                   index 27ac6d3..7576e3e 100644
                   --- a/path/to/original
                   +++ b/path/to/new
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        AssertPatchHasSingleEntryWithHeader<DiffGitPatchEntryHeader>(text);
    }

    [Fact]
    public void Patch_Header_Index_CanBeParsed()
    {
        var text = """
                   diff --git a/example.txt b/example.txt
                   {:index 27ac6d3..7576e3e 100644:}
                   --- a/path/to/original
                   +++ b/path/to/new
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<IndexPatchEntryHeader>(text);

        header.Mode.Should().Be(Convert.ToInt32("100644", 8));
    }

    [Fact]
    public void Patch_Header_OldPath_CanBeParsed()
    {
        var text = """
                   diff --git a/example.txt b/example.txt
                   index 27ac6d3..7576e3e 100644
                   {:--- a/path/to/original:}
                   +++ b/path/to/new
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<OldPathPatchEntryHeader>(text);

        header.Path.Should().Be("path/to/original");
    }

    [Fact]
    public void Patch_Header_NewPath_CanBeParsed()
    {
        var text = """
                   diff --git a/example.txt b/example.txt
                   index 27ac6d3..7576e3e 100644
                   --- a/path/to/original
                   {:+++ b/path/to/new:}
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<NewPathPatchEntryHeader>(text);
        header.Path.Should().Be("path/to/new");
    }

    [Fact]
    public void Patch_Header_OldMode_CanBeParsed()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   {:old mode 100755:}
                   new mode 100644
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<OldModePatchEntryHeader>(text);
        header.Mode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Header_NewMode_CanBeParsed()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   old mode 100755
                   {:new mode 100644:}
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<NewModePatchEntryHeader>(text);
        header.Mode.Should().Be(Convert.ToInt32("100644", 8));
    }

    [Fact]
    public void Patch_Header_DeletedFileMode_CanBeParsed()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   {:deleted file mode 100755:}
                   index ebfac67..0000000
                   --- a/hello.sh
                   +++ b/dev/null
                   @@ -1,3 +0,0 @@
                   -#!/bin/bash
                   -
                   -echo Hello
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<DeletedFileModePatchEntryHeader>(text);
        header.Mode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Header_NewFileMode_CanBeParsed()
    {
        var text = """
                   diff --git a/hello.sh b/hello.sh
                   {:new file mode 100755:}
                   index 0000000..ebfac67
                   --- a/dev/null
                   +++ b/hello.sh
                   @@ -0,0 +1,3 @@
                   +#!/bin/bash
                   +
                   +echo Hello
                   """;

        var header = AssertPatchHasSingleEntryWithHeader<NewFileModePatchEntryHeader>(text);
        header.Mode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Header_UnknownHeader_CanBeParsed()
    {
        var text = """
                   diff --git a/example.txt b/example.txt
                   index 27ac6d3..7576e3e 100644
                   {:an unknown header is here:}
                   --- a/path/to/original
                   +++ b/path/to/new
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        AssertPatchHasSingleEntryWithHeader<UnknownPatchEntryHeader>(text);
    }

    // TODO: Test CopyFromPatchEntryHeader
    // TODO: Test CopyToPatchEntryHeader
    // TODO: Test RenameFromPatchEntryHeader
    // TODO: Test RenameToPatchEntryHeader
    // TODO: Test SimilarityIndexPatchEntryHeader
    // TODO: Test DissimilarityIndexPatchEntryHeader

    [Fact]
    public void Patch_Entry_OldPath_SetWhenFileIsUpdated()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   index c2b72f4..ebfac67 100755
                   --- a/build.sh
                   +++ b/build.sh
                   @@ -1,10 +1,3 @@
                    #!/bin/bash

                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldPath.Should().Be("build.sh");
    }

    [Fact]
    public void Patch_Entry_OldPath_SetWhenFileIsRenamed()
    {
        var text = """
                   diff --git a/build.sh b/hello.sh
                   similarity index 100%
                   rename from build.sh
                   rename to hello.sh
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldPath.Should().Be("build.sh");
    }

    [Fact]
    public void Patch_Entry_OldPath_SetWhenFileIsAdded()
    {
        var text = """
                   diff --git a/hello.sh b/hello.sh
                   new file mode 100755
                   index 0000000..ebfac67
                   --- /dev/null
                   +++ b/hello.sh
                   @@ -0,0 +1,3 @@
                   +#!/bin/bash
                   +
                   +echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldPath.Should().Be("");
    }

    [Fact]
    public void Patch_Entry_OldPath_SetWhenFileIsDeleted()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   deleted file mode 100755
                   index c2b72f4..0000000
                   --- a/build.sh
                   +++ b/dev/null
                   @@ -1,10 +0,0 @@
                   -#!/bin/bash
                   -
                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldPath.Should().Be("build.sh");
    }

    [Fact]
    public void Patch_Entry_NewPath_SetWhenFileIsUpdated()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   index c2b72f4..ebfac67 100755
                   --- a/build.sh
                   +++ b/build.sh
                   @@ -1,10 +1,3 @@
                    #!/bin/bash

                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewPath.Should().Be("build.sh");
    }

    [Fact]
    public void Patch_Entry_NewPath_SetWhenFileIsRenamed()
    {
        var text = """
                   diff --git a/build.sh b/hello.sh
                   similarity index 100%
                   rename from build.sh
                   rename to hello.sh
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewPath.Should().Be("hello.sh");
    }

    [Fact]
    public void Patch_Entry_NewPath_SetWhenFileIsAdded()
    {
        var text = """
                   diff --git a/hello.sh b/hello.sh
                   new file mode 100755
                   index 0000000..ebfac67
                   --- /dev/null
                   +++ b/hello.sh
                   @@ -0,0 +1,3 @@
                   +#!/bin/bash
                   +
                   +echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewPath.Should().Be("hello.sh");
    }

    [Fact]
    public void Patch_Entry_NewPath_SetWhenFileIsDeleted()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   deleted file mode 100755
                   index c2b72f4..0000000
                   --- a/build.sh
                   +++ b/dev/null
                   @@ -1,10 +0,0 @@
                   -#!/bin/bash
                   -
                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewPath.Should().Be("");
    }

    [Fact]
    public void Patch_Entry_OldMode_SetWhenFileIsUpdated()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   index c2b72f4..ebfac67 100755
                   --- a/build.sh
                   +++ b/build.sh
                   @@ -1,10 +1,3 @@
                    #!/bin/bash

                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldMode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Entry_OldMode_SetWhenFileIsRenamed()
    {
        var text = """
                   diff --git a/build.sh b/hello.sh
                   similarity index 100%
                   rename from build.sh
                   rename to hello.sh
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldMode.Should().Be(0);
    }

    [Fact]
    public void Patch_Entry_OldMode_SetWhenFileIsAdded()
    {
        var text = """
                   diff --git a/hello.sh b/hello.sh
                   new file mode 100755
                   index 0000000..ebfac67
                   --- /dev/null
                   +++ b/hello.sh
                   @@ -0,0 +1,3 @@
                   +#!/bin/bash
                   +
                   +echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldMode.Should().Be(0);
    }

    [Fact]
    public void Patch_Entry_OldMode_SetWhenFileIsDeleted()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   deleted file mode 100755
                   index c2b72f4..0000000
                   --- a/build.sh
                   +++ b/dev/null
                   @@ -1,10 +0,0 @@
                   -#!/bin/bash
                   -
                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.OldMode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Entry_NewMode_SetWhenFileIsUpdated()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   index c2b72f4..ebfac67 100755
                   --- a/build.sh
                   +++ b/build.sh
                   @@ -1,10 +1,3 @@
                    #!/bin/bash

                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewMode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Entry_NewMode_SetWhenFileIsRenamed()
    {
        var text = """
                   diff --git a/build.sh b/hello.sh
                   similarity index 100%
                   rename from build.sh
                   rename to hello.sh
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewMode.Should().Be(0);
    }

    [Fact]
    public void Patch_Entry_NewMode_SetWhenFileIsAdded()
    {
        var text = """
                   diff --git a/hello.sh b/hello.sh
                   new file mode 100755
                   index 0000000..ebfac67
                   --- /dev/null
                   +++ b/hello.sh
                   @@ -0,0 +1,3 @@
                   +#!/bin/bash
                   +
                   +echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewMode.Should().Be(Convert.ToInt32("100755", 8));
    }

    [Fact]
    public void Patch_Entry_NewMode_SetWhenFileIsDeleted()
    {
        var text = """
                   diff --git a/build.sh b/build.sh
                   deleted file mode 100755
                   index c2b72f4..0000000
                   --- a/build.sh
                   +++ b/dev/null
                   @@ -1,10 +0,0 @@
                   -#!/bin/bash
                   -
                   -echo Hello
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.NewMode.Should().Be(0);
    }

    [Fact]
    public void Patch_Hunk_OldStartLength_CanBeParsed()
    {
        var text = """
                   diff --git a/lorem.txt b/lorem.txt
                   index 4fcc812..7df7b9f 100644
                   --- a/lorem.txt
                   +++ b/lorem.txt
                   @@ -3,8 +3,7 @@
                    Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod
                    tempor incididunt ut labore et dolore magna aliqua.

                   -Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
                   -aliquip ex ea commodo consequat.
                   +Change

                    Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore
                    eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident,
                   """;

        var hunk = AssertPatchHasSingleHunk(text);

        hunk.Header.Should().NotBeNull();
        hunk.Header.OldLine.Should().Be(3);
        hunk.Header.OldCount.Should().Be(8);
    }

    [Fact]
    public void Patch_Hunk_OldStartWithoutLength_CanBeParsed()
    {
        var text = """
                   diff --git a/lorem.txt b/lorem.txt
                   index 4fcc812..d3fb671 100644
                   --- a/lorem.txt
                   +++ b/lorem.txt
                   @@ -3 +4,5 @@
                   +Addition
                   """;

        var hunk = AssertPatchHasSingleHunk(text);

        hunk.Header.Should().NotBeNull();
        hunk.Header.OldLine.Should().Be(3);
        hunk.Header.OldCount.Should().Be(1);
    }
    [Fact]
    public void Patch_Hunk_NewStartLength_CanBeParsed()
    {
        var text = """
                   diff --git a/lorem.txt b/lorem.txt
                   index 4fcc812..d3fb671 100644
                   --- a/lorem.txt
                   +++ b/lorem.txt
                   @@ -1,3 +1,5 @@
                   +Addition
                   +
                    # Lorem

                    Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod
                   @@ -8,4 +10,4 @@ aliquip ex ea commodo consequat.

                    Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore
                    eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident,
                   -sunt in culpa qui officia deserunt mollit anim id est laborum.
                   \ No newline at end of file
                   +change
                   """;

        var entry = AssertPatchHasSingleEntry(text);
        entry.Hunks.Should().HaveCount(2);

        var hunk = entry.Hunks[1];

        hunk.Header.Should().NotBeNull();
        hunk.Header.NewLine.Should().Be(10);
        hunk.Header.NewCount.Should().Be(4);
    }

    [Fact]
    public void Patch_Hunk_NewStartWithoutLength_CanBeParsed()
    {
        var text = """
                   diff --git a/lorem.txt b/lorem.txt
                   index 4fcc812..d3fb671 100644
                   --- a/lorem.txt
                   +++ b/lorem.txt
                   @@ -1,2 +4 @@
                   +Addition
                   """;

        var hunk = AssertPatchHasSingleHunk(text);

        hunk.Header.Should().NotBeNull();
        hunk.Header.NewLine.Should().Be(4);
        hunk.Header.NewCount.Should().Be(1);
    }

    [Fact]
    public void Patch_Hunk_Function_CanBeParsed()
    {
        var text = """
                   diff --git a/lorem.txt b/lorem.txt
                   index 4fcc812..7df7b9f 100644
                   --- a/lorem.txt
                   +++ b/lorem.txt
                   @@ -1,2 +3,4 @@ void Main()
                   + Test
                   """;

        var hunk = AssertPatchHasSingleHunk(text);

        hunk.Header.Function.Should().Be("void Main()");
    }

    [Fact]
    public void Patch_HunkLine_Added_CanBeParsed()
    {
        var text = """
                   diff --git a/hello.txt b/hello.txt
                   new file mode 100644
                   index 0000000..345e6ae
                   --- /dev/null
                   +++ b/hello.txt
                   @@ -0,0 +1 @@
                    Some Context
                   -Something Old
                   {:+Something New:}
                   """;

        AssertPatchHasSingleEntryWithHunkLine(text, PatchNodeKind.AddedLine);
    }

    [Fact]
    public void Patch_HunkLine_Deleted_CanBeParsed()
    {
        var text = """
                   diff --git a/hello.txt b/hello.txt
                   new file mode 100644
                   index 0000000..345e6ae
                   --- /dev/null
                   +++ b/hello.txt
                   @@ -0,0 +1 @@
                    Some Context
                   {:-Something Old:}
                   +Something New
                   """;

        AssertPatchHasSingleEntryWithHunkLine(text, PatchNodeKind.DeletedLine);
    }

    [Fact]
    public void Patch_HunkLine_Context_CanBeParsed()
    {
        var text = """
                   diff --git a/hello.txt b/hello.txt
                   new file mode 100644
                   index 0000000..345e6ae
                   --- /dev/null
                   +++ b/hello.txt
                   @@ -0,0 +1 @@
                   {: Some Context:}
                   -Something Old
                   +Something New
                   """;

        AssertPatchHasSingleEntryWithHunkLine(text, PatchNodeKind.ContextLine);
    }

    [Fact]
    public void Patch_HunkLine_NoNewLine_CanBeParsed()
    {
        var text = """
                   diff --git a/hello.txt b/hello.txt
                   new file mode 100644
                   index 0000000..8318c86
                   --- /dev/null
                   +++ b/hello.txt
                   @@ -0,0 +1 @@
                   +Test
                   {:\ No newline at end of file:}
                   """;

        AssertPatchHasSingleEntryWithHunkLine(text, PatchNodeKind.NoNewLine);
    }

    [Fact]
    public void Patch_Diff_Cc_IsParsedAsConflict()
    {
        var text = """
                   diff --git a/before.txt b/before.txt
                   index 27ac6d3..7576e3e 100644
                   --- a/before.txt
                   +++ b/before.txt
                   @@ -1,0 +1,1 @@
                   +This is an important
                   diff --cc conflicted.txt
                   index 14eb246,98d39d3..0000000
                   --- a/conflicted.txt
                   +++ b/conflicted.txt
                   @@@ -2,7 -2,7 +2,11 @@@ Additio
                   
                     # Lorem
                   
                   ++<<<<<<< HEAD
                    +Change in Main
                   ++=======
                   + Change in Test
                   ++>>>>>>> de5c834 (Change in branch Test)
                   
                     Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
                     aliquip ex ea commodo consequat.
                   diff --git a/after.txt b/after.txt
                   index 27ac6d3..7576e3e 100644
                   --- a/after.txt
                   +++ b/after.txt
                   @@ -1,0 +1,1 @@
                   +This is an important
                   """;

        var expectedText = """
                           diff --git a/before.txt b/before.txt
                           index 27ac6d3..7576e3e 100644
                           --- a/before.txt
                           +++ b/before.txt
                           @@ -1,0 +1,1 @@
                           +This is an important
                           diff --git a/conflicted.txt b/conflicted.txt
                           !needs merge
                           diff --git a/after.txt b/after.txt
                           index 27ac6d3..7576e3e 100644
                           --- a/after.txt
                           +++ b/after.txt
                           @@ -1,0 +1,1 @@
                           +This is an important
                           """;

        var actualText = Patch.Parse(text).ToString();
        
        actualText.Should().Be(expectedText);
    }

    private static PatchEntry AssertPatchHasSingleEntry(string text)
    {
        var patch = Patch.Parse(text);

        patch.Entries.Should().HaveCount(1);

        return patch.Entries[0];
    }

    private static PatchHunk AssertPatchHasSingleHunk(string text)
    {
        var entry = AssertPatchHasSingleEntry(text);

        entry.Hunks.Should().HaveCount(1);

        return entry.Hunks[0];
    }

    private static T AssertPatchHasSingleEntryWithHeader<T>(string textWithMarkedSpan)
        where T: PatchEntryHeader
    {
        ParseTextWithSpan(textWithMarkedSpan, out var text, out var markedSpan);

        var patch = Patch.Parse(text);

        var markedLineIndex = patch.Text.GetLineIndex(markedSpan.Start);
        var headerIndex = markedLineIndex;

        patch.Entries.Should().HaveCount(1);

        var entry = patch.Entries[0];
        entry.Headers.Should().HaveCountGreaterThan(headerIndex);

        var header = entry.Headers[headerIndex].Should().BeOfType<T>().Subject;

        var expectedKind = GetExpectedKind<T>();
        header.Kind.Should().Be(expectedKind);

        var expectedHeaderText = text.Substring(markedSpan);
        var actualHeaderSpan = text.Substring(header.Span);
        actualHeaderSpan.Should().Be(expectedHeaderText);

        return header;
    }

    private static void AssertPatchHasSingleEntryWithHunkLine(string textWithMarkedSpan, PatchNodeKind expectedKind)
    {
        ParseTextWithSpan(textWithMarkedSpan, out var text, out var markedSpan);

        var patch = Patch.Parse(text);

        var markedLineIndex = patch.Text.GetLineIndex(markedSpan.Start);
        var headerIndex = markedLineIndex;

        patch.Entries.Should().HaveCount(1);

        var entry = patch.Entries[0];

        entry.Hunks.Should().HaveCount(1);

        var hunk = entry.Hunks[0];

        var line = hunk.Lines.Should().Contain(l => l.Kind == expectedKind).Which;

        var expectedText = text.Substring(markedSpan);
        var actualText = text.Substring(line.Span);
        actualText.Should().Be(expectedText);
    }

    private static PatchNodeKind GetExpectedKind<T>()
    {
        if (typeof(T) == typeof(UnknownPatchEntryHeader))
            return PatchNodeKind.UnknownHeader;

        if (typeof(T) == typeof(DiffGitPatchEntryHeader))
            return PatchNodeKind.DiffGitHeader;

        if (typeof(T) == typeof(OldPathPatchEntryHeader))
            return PatchNodeKind.OldPathHeader;

        if (typeof(T) == typeof(NewPathPatchEntryHeader))
            return PatchNodeKind.NewPathHeader;

        if (typeof(T) == typeof(OldModePatchEntryHeader))
            return PatchNodeKind.OldModeHeader;

        if (typeof(T) == typeof(NewModePatchEntryHeader))
            return PatchNodeKind.NewModeHeader;

        if (typeof(T) == typeof(DeletedFileModePatchEntryHeader))
            return PatchNodeKind.DeletedFileModeHeader;

        if (typeof(T) == typeof(NewFileModePatchEntryHeader))
            return PatchNodeKind.NewFileModeHeader;

        if (typeof(T) == typeof(CopyFromPatchEntryHeader))
            return PatchNodeKind.CopyFromHeader;

        if (typeof(T) == typeof(CopyToPatchEntryHeader))
            return PatchNodeKind.CopyToHeader;

        if (typeof(T) == typeof(RenameFromPatchEntryHeader))
            return PatchNodeKind.RenameFromHeader;

        if (typeof(T) == typeof(RenameToPatchEntryHeader))
            return PatchNodeKind.RenameToHeader;

        if (typeof(T) == typeof(SimilarityIndexPatchEntryHeader))
            return PatchNodeKind.SimilarityIndexHeader;

        if (typeof(T) == typeof(DissimilarityIndexPatchEntryHeader))
            return PatchNodeKind.DissimilarityIndexHeader;

        if (typeof(T) == typeof(IndexPatchEntryHeader))
            return PatchNodeKind.IndexHeader;

        throw new UnreachableException($"Unexpected type {typeof(T)}");
    }

    private static void ParseTextWithSpan(string text, out string textWithoutMarker, out TextSpan span)
    {
        const string markerStart = "{:";
        const string markerEnd = ":}";

        var indexOfStart = text.IndexOf(markerStart, StringComparison.Ordinal);
        var indexOfEnd = text.IndexOf(markerEnd, StringComparison.Ordinal);

        indexOfStart.Should().BeGreaterThanOrEqualTo(0);
        indexOfEnd.Should().BeGreaterThanOrEqualTo(indexOfStart + markerStart.Length);

        textWithoutMarker = text.Remove(new TextSpan(indexOfEnd, markerEnd.Length))
                                .Remove(new TextSpan(indexOfStart, markerStart.Length));

        var hasRemainingMarkers = textWithoutMarker.Contains(markerStart, StringComparison.Ordinal) ||
                                  textWithoutMarker.Contains(markerEnd, StringComparison.Ordinal);

        if (hasRemainingMarkers)
            throw new Exception("You can only mark a single span.");

        span = TextSpan.FromBounds(indexOfStart, indexOfEnd - markerStart.Length);
    }
}