using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class InProgressIndicator
{
    [Inject]
    public InProgressIndicatorService? Service { get; set; }

    private bool InProgress;
    private string? InProgressText;

    protected override void OnInitialized()
    {
        Service!.OnInProgress = (text) =>
        {
            if (!InProgress)
            {
                InProgress = true;
            }
            InProgressText = text;
            StateHasChanged();
        };

        Service.OnCompleteProgress = () =>
        {
            InProgress = false;
            InProgressText = null;
            StateHasChanged();
        };
    }
}
