
using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentsComparison
{
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [SupplyParameterFromQuery(Name = "runs")]
    public string? Runs { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private ILogger<ExperimentsComparison>? Logger { get; set; }

    [Inject]
    private InProgressIndicatorService? InProgressIndicator { get; set; }

    private List<ExperimentRun>? experimentRuns;
    private const string TabSelectedKey = $"{nameof(ExperimentsComparison)}TabKey";
    private int tabSelectedIndex;

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentsComparison), out var experimentRunsObj))
        {
            experimentRuns = (List<ExperimentRun>)experimentRunsObj;
        }
        else
        {
            if (string.IsNullOrEmpty(Runs))
            {
                Logger!.LogError("no runs provided");
                NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
                return;
            }

            InProgressIndicator!.Show("loading experiments");

            List<ExperimentRun> runs = [];
            var runIds = Runs.Split(',').Select(x => Guid.Parse(x)).ToList();
            foreach (var runId in runIds)
            {
                var response = await Client!.GetExperimentRunAsync(ProjectId, ExperimentId, runId);
                if (response.Success)
                {
                    runs.Add(response.Result!);
                }
                else
                {
                    Logger!.LogError("unable to get experiment run: {runId}, error: {error}", runId, response.ErrorMessage);
                }
            }
            experimentRuns = runs;
            InProgressIndicator!.Hide();
        }

        if (UserSession.Items.TryGetValue(TabSelectedKey, out var tabObj) && tabObj is int tabIndex && tabIndex > 0)
        {
            tabSelectedIndex = tabIndex;
        }
    }

    private void Back()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
    }

    private void OnTabChanged(int index)
    {
        UserSession!.Items[TabSelectedKey] = index;
    }
}