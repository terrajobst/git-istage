namespace GitIStage.Tests;

public class PatchEntryKeyTests
{
    private const string TwoEntryPatch = """
                                         diff --git a/file1.txt b/file1.txt
                                         index 1234567..abcdef0 100644
                                         --- a/file1.txt
                                         +++ b/file1.txt
                                         @@ -1,3 +1,3 @@
                                          aaa
                                         -bbb
                                         +ccc
                                          ddd
                                         diff --git a/file2.txt b/file2.txt
                                         index 2345678..bcdef01 100644
                                         --- a/file2.txt
                                         +++ b/file2.txt
                                         @@ -1,2 +1,2 @@
                                          xxx
                                         -yyy
                                         +zzz
                                         """;

    [Fact]
    public void PatchEntryKey_SameEntry_AreEqual()
    {
        var patch = Patch.Parse(TwoEntryPatch);
        var key1 = new PatchEntryKey(patch.Entries[0]);
        var key2 = new PatchEntryKey(patch.Entries[0]);

        key1.Equals(key2).Should().BeTrue();
        (key1 == key2).Should().BeTrue();
        (key1 != key2).Should().BeFalse();
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void PatchEntryKey_DifferentEntries_AreNotEqual()
    {
        var patch = Patch.Parse(TwoEntryPatch);
        var key1 = new PatchEntryKey(patch.Entries[0]);
        var key2 = new PatchEntryKey(patch.Entries[1]);

        key1.Equals(key2).Should().BeFalse();
        (key1 != key2).Should().BeTrue();
    }

    [Fact]
    public void PatchEntryKey_IdenticalEntriesFromDifferentParses_AreEqual()
    {
        var patch1 = Patch.Parse(TwoEntryPatch);
        var patch2 = Patch.Parse(TwoEntryPatch);

        var key1 = new PatchEntryKey(patch1.Entries[0]);
        var key2 = new PatchEntryKey(patch2.Entries[0]);

        key1.Equals(key2).Should().BeTrue();
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void PatchEntryKey_ImplicitConversionFromEntry()
    {
        var patch = Patch.Parse(TwoEntryPatch);

        PatchEntryKey key = patch.Entries[0];

        key.Equals(new PatchEntryKey(patch.Entries[0])).Should().BeTrue();
    }

    [Fact]
    public void PatchEntryKey_ImplicitConversionToEntry()
    {
        var patch = Patch.Parse(TwoEntryPatch);
        PatchEntryKey key = patch.Entries[0];

        PatchEntry entry = key;

        entry.Should().BeSameAs(patch.Entries[0]);
    }

    [Fact]
    public void PatchEntryKey_Equals_BoxedObject()
    {
        var patch = Patch.Parse(TwoEntryPatch);
        var key1 = new PatchEntryKey(patch.Entries[0]);
        var key2 = new PatchEntryKey(patch.Entries[0]);

        key1.Equals((object)key2).Should().BeTrue();
    }

    [Fact]
    public void PatchEntryKey_Equals_NonPatchEntryKey_ReturnsFalse()
    {
        var patch = Patch.Parse(TwoEntryPatch);
        var key = new PatchEntryKey(patch.Entries[0]);

        key.Equals("not a key").Should().BeFalse();
    }

    [Fact]
    public void PatchEntryKey_GetSet_ReturnsAllEntries()
    {
        var patch = Patch.Parse(TwoEntryPatch);

        var set = PatchEntryKey.GetSet(patch);

        set.Should().HaveCount(2);
        set.Contains(patch.Entries[0]).Should().BeTrue();
        set.Contains(patch.Entries[1]).Should().BeTrue();
    }

    [Fact]
    public void PatchEntryKey_GetSet_CanFindEntriesFromDifferentParse()
    {
        var patch1 = Patch.Parse(TwoEntryPatch);
        var patch2 = Patch.Parse(TwoEntryPatch);

        var set = PatchEntryKey.GetSet(patch1);

        set.Contains(patch2.Entries[0]).Should().BeTrue();
        set.Contains(patch2.Entries[1]).Should().BeTrue();
    }

    [Fact]
    public void PatchEntryKey_GetSet_DoesNotFindModifiedEntry()
    {
        var patch1 = Patch.Parse(TwoEntryPatch);

        var modifiedPatch = """
                            diff --git a/file1.txt b/file1.txt
                            index 1234567..abcdef0 100644
                            --- a/file1.txt
                            +++ b/file1.txt
                            @@ -1,3 +1,3 @@
                             aaa
                            -bbb
                            +DIFFERENT
                             ddd
                            """;

        var patch2 = Patch.Parse(modifiedPatch);
        var set = PatchEntryKey.GetSet(patch1);

        set.Contains(patch2.Entries[0]).Should().BeFalse();
    }

    [Fact]
    public void PatchEntryKey_GetSet_EmptyPatch_ReturnsEmptySet()
    {
        var set = PatchEntryKey.GetSet(Patch.Empty);

        set.Should().BeEmpty();
    }
}
