namespace ExperimentServer.Services;

public class InProgressIndicatorService(ILogger<InProgressIndicatorService> logger)
{

    public Action<string>? OnInProgress { get; set; }

    public Action? OnCompleteProgress { get; set; }

    private int count;
    public void Show(string message)
    {
        if (count == 0)
        {
            OnInProgress!(message);
        }
        count++;

        if (count > 1)
        {
            logger.LogWarning("[InProgressIndicatorService] show called more than once, count currently at {count}", count);
        }
    }

    public void Hide()
    {
        count--;

        if (count > 0)
        {
            logger.LogWarning("[InProgressIndicatorService] hide called but count is currently at {count}", count);
            return;
        }

        OnCompleteProgress!();
    }
}
