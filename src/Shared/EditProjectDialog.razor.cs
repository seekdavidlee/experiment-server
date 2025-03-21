using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Text.Json;

namespace ExperimentServer.Shared;

public partial class EditProjectDialog
{
    [Parameter]
    public ProjectModel? Model { get; set; }

    [Parameter]
    public List<ProjectModel>? Models { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private string? ErrorMessage;

    private bool IsSaving { get; set; }

    private ProjectModel? WorkingModel { get; set; }

    protected override void OnInitialized()
    {
        WorkingModel = JsonSerializer.Deserialize<ProjectModel>(JsonSerializer.Serialize(Model));
    }

    private async Task SaveAsync()
    {
        if (WorkingModel is null || Models is null) return;

        if (string.IsNullOrEmpty(WorkingModel.DisplayName))
        {
            ErrorMessage = "please enter a valid display name";
            return;
        }
        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        if (WorkingModel.Id == Guid.Empty)
        {
            WorkingModel.Id = Guid.NewGuid();
        }

        Models = Models.Where(x => x.Id != WorkingModel.Id).ToList();
        Models.Add(WorkingModel);

        var response = await Client!.SaveProjectsAsync(Models);
        IsSaving = false;

        if (!response.Success)
        {
            ErrorMessage = response.ErrorMessage;
            StateHasChanged();
            return;
        }
        DialogService!.Close(true);
    }

    private void Cancel()
    {
        DialogService!.Close(false);
    }
}
