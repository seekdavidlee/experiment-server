using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentRunDetail
{
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [Parameter]
    public Guid RunId { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    private ExperimentRun? model;

    private List<ExperimentLog>? Logs { get; set; }

    private List<ExperimentRunResult>? Results { get; set; }

    private List<ExperimentMetric>? Metrics { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync(true);
    }

    public async Task RefreshAsync(bool useSessionModel)
    {
        ErrorMessage = null;

        bool foundSessionModel = false;
        if (useSessionModel && UserSession!.Items.TryGetValue(nameof(ExperimentRun), out var modelObj) && modelObj is not null)
        {
            model = (ExperimentRun)modelObj;
            foundSessionModel = true;
        }

        if (!foundSessionModel)
        {
            var response = await Client!.GetExperimentRunAsync(ProjectId, ExperimentId, RunId);
            if (response.Success)
            {
                model = response.Result;
            }
            else
            {
                ErrorMessage = "Failed to load experiment run.";
                return;
            }
        }

        Logs = await Client!.GetExperimentRunLogsAsync(ProjectId, ExperimentId, RunId);
        Results = await Client!.GetExperimentRunResultsAsync(ProjectId, ExperimentId, RunId);
        Metrics = await Client!.GetExperimentRunMetricsAsync(ProjectId, ExperimentId, RunId);
    }

    private void Back()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
    }

    public void OpenImageAsync(ExperimentRunResult result, ExperimentMetric metric)
    {
        UserSession!.Items[nameof(ExperimentRunResult)] = result;
        UserSession!.Items[nameof(ExperimentMetric)] = metric;
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{RunId}/detail");
    }
}
