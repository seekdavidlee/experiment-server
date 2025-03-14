using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Radzen;

namespace ExperimentServer.Pages.Projects;

public partial class ProjectsDashboard
{
    private RadzenDataGrid<ProjectModel>? dataGrid;

    private readonly ProjectDashboardModel model = new();

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetProjectsAsync());
        await dataGrid!.Reload();
    }

    private async Task Edit(ProjectModel projectModel)
    {
        bool? result = await DialogService!.OpenAsync<EditProjectDialog>("Edit Project", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", projectModel },
             { "Models", model.Items },
             { "Client", Client! }
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

    private async Task AddNew()
    {
        bool? result = await DialogService!.OpenAsync<EditProjectDialog>("New Project", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", new ProjectModel() },
             { "Models", model.Items },
             { "Client", Client! }
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
