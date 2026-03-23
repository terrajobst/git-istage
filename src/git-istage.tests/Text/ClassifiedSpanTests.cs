namespace GitIStage.Tests;

public class ClassifiedSpanTests
{
    [Fact]
    public void ClassifiedSpan_ClassificationConstructor_SetsProperties()
    {
        var span = new TextSpan(5, 10);

        var classifiedSpan = new ClassifiedSpan(span, PatchClassification.AddedLine);

        classifiedSpan.Span.Should().Be(span);
        classifiedSpan.Classification.Should().Be(PatchClassification.AddedLine);
    }

    [Fact]
    public void ClassifiedSpan_ScopesClassification_SetsProperties()
    {
        var span = new TextSpan(5, 10);
        var classification = Classification.Create("source.cs", "keyword.control");

        var classifiedSpan = new ClassifiedSpan(span, classification);

        classifiedSpan.Span.Should().Be(span);
        classifiedSpan.Classification.Should().Be(classification);
        classifiedSpan.Classification.Scopes.Length.Should().Be(2);
    }

    [Fact]
    public void ClassifiedSpan_Equal_WhenSameClassification()
    {
        var span = new TextSpan(0, 5);
        var s1 = new ClassifiedSpan(span, PatchClassification.Keyword);
        var s2 = new ClassifiedSpan(span, PatchClassification.Keyword);

        s1.Should().Be(s2);
        s1.GetHashCode().Should().Be(s2.GetHashCode());
    }

    [Fact]
    public void ClassifiedSpan_Equal_WhenSameInternedClassification()
    {
        var span = new TextSpan(0, 5);
        var s1 = new ClassifiedSpan(span, Classification.Create("source.cs", "keyword"));
        var s2 = new ClassifiedSpan(span, Classification.Create("source.cs", "keyword"));

        s1.Should().Be(s2);
    }

    [Fact]
    public void ClassifiedSpan_NotEqual_WhenSpansDiffer()
    {
        var s1 = new ClassifiedSpan(new TextSpan(0, 5), PatchClassification.Keyword);
        var s2 = new ClassifiedSpan(new TextSpan(1, 5), PatchClassification.Keyword);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void ClassifiedSpan_NotEqual_WhenClassificationsDiffer()
    {
        var span = new TextSpan(0, 5);
        var s1 = new ClassifiedSpan(span, PatchClassification.Keyword);
        var s2 = new ClassifiedSpan(span, PatchClassification.PathToken);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void ClassifiedSpan_Operators_Work()
    {
        var s1 = new ClassifiedSpan(new TextSpan(0, 5), PatchClassification.PathToken);
        var s2 = new ClassifiedSpan(new TextSpan(0, 5), PatchClassification.PathToken);
        var s3 = new ClassifiedSpan(new TextSpan(0, 6), PatchClassification.PathToken);

        (s1 == s2).Should().BeTrue();
        (s1 != s3).Should().BeTrue();
    }
}
