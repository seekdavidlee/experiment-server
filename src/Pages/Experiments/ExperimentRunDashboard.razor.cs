using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentRunDashboard
{
    private RadzenDataGrid<ExperimentRunModel>? dataGrid;

    private readonly ExperimentRunDashboardModel model = new();

    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    private ExperimentModel? experimentModel;

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentModel), out var experimentModelOut))
        {
            experimentModel = (ExperimentModel)experimentModelOut;
        }
        await RefreshAsync();
    }

    private async Task RunAsync()
    {
        bool? result = await DialogService!.OpenAsync<RunExperimentDialog>("New run", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "ExperimentId", ExperimentId! },
             { "ProjectId", ProjectId! },
             { "Client", Client! },
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "90%",
            Height = "700px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        var results = await Client!.GetExperimentRunsAsync(ProjectId, ExperimentId);
        model.Items.AddRange(results.Select(x => new ExperimentRunModel { Value = x }));
        await dataGrid!.Reload();
    }

    private void View(ExperimentRun experimentRun)
    {
        var path = $"projects/{ProjectId}/experiments/{ExperimentId}/runs/{experimentRun.Id}";
        UserSession!.Items[nameof(ExperimentRun)] = experimentRun;
        NavigationManager!.NavigateTo(path);
    }

    private async Task DeleteAsync(ExperimentRun experimentRun)
    {
        ErrorMessage = null;
        var result = await DialogService!.Confirm($"Are you sure you want to delete {experimentRun.Id}?", "Delete run", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
        if (result is not null && result == true)
        {
            var response = await Client!.DeleteExperimentRunAsync(ProjectId, ExperimentId, experimentRun.Id);
            if (response.Success)
            {
                await RefreshAsync();
            }
            else
            {
                ErrorMessage = response.ErrorMessage;
            }
        }
    }

    private void PerformComparisons()
    {
        List<ExperimentRun> runs = [];
        foreach (var item in model.Items)
        {
            if (item.IsSelected)
            {
                runs.Add(item.Value!);
            }
        }

        UserSession!.Items[nameof(ExperimentsComparison)] = runs;
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/comparisons");
    }

    private void Back()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments");
    }
}
