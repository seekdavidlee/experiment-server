using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentsDashboard
{
    private RadzenDataGrid<ExperimentModel>? dataGrid;

    private readonly ExperimentsDashboardModel model = new();

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    [Parameter]
    public Guid ProjectId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetExperimentsAsync(ProjectId!));
        await dataGrid!.Reload();
    }

    private async Task Edit(ExperimentModel model)
    {
        bool? result = await DialogService!.OpenAsync<EditExperimentDialog>("Edit Experiment", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", model },
             { "Models", this.model.Items },
             { "Client", Client! },
             { "ProjectId", ProjectId }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "400px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }

    private async Task RunAsync()
    {
        bool? result = await DialogService!.OpenAsync<EditExperimentDialog>("New Experiment", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", new ExperimentModel() },
             { "Models", model.Items },
             { "Client", Client! },
             { "ProjectId", ProjectId }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "400px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }
}
