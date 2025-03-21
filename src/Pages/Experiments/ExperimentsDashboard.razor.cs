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

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Parameter]
    public Guid ProjectId { get; set; }

    private ProjectModel? projectModel;

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ProjectModel), out var projectModelOut))
        {
            projectModel = (ProjectModel)projectModelOut;
        }
        else
        {
            // load project model
            var response = await Client!.GetProjectAsync(ProjectId);
            if (response.Success)
            {
                projectModel = response.Result;
            }
            else
            {
                ErrorMessage = response.ErrorMessage;
            }
        }
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetExperimentsAsync(ProjectId!));
        await dataGrid!.Reload();
    }

    private void Open(ExperimentModel model)
    {
        var path = $"projects/{ProjectId}/experiments/{model.Id}/runs";
        UserSession!.Items[nameof(ExperimentModel)] = model;
        NavigationManager!.NavigateTo(path);
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

    private async Task NewAsync()
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

    private async Task DeleteAsync(ExperimentModel experimentModel)
    {
        ErrorMessage = null;
        var result = await DialogService!.Confirm($"Are you sure you want to delete <b>{experimentModel.Name}</b>? All runs will also be removed.", "Delete experiment", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
        if (result is not null && result == true)
        {
            var response = await Client!.DeleteExperimentAsync(ProjectId, experimentModel);
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

    private void Back()
    {
        NavigationManager!.NavigateTo("/");
    }
}