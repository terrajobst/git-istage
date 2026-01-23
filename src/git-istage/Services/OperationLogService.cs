using GitIStage.UI;

namespace GitIStage.Services;

internal sealed class OperationLogService
{
    public LogDocument Document { get; private set; } = LogDocument.Empty;

    public bool LastUpdateHadErrors { get; private set; }

    public void Log(IReadOnlyCollection<GitOperation> operations)
    {
        Document = Document.Prepend(operations);
        LastUpdateHadErrors = operations.Any(o => o.Result is not null && !o.Result.Success);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;
}