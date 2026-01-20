using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class ErrorDocument : Document
{
    private ErrorDocument(SourceText sourceText)
        : base(sourceText)
    {
    }

    public static ErrorDocument Create(Exception exception)
    {
        var sourceText = SourceText.From(exception.ToString());
        return new ErrorDocument(sourceText);
    }
}