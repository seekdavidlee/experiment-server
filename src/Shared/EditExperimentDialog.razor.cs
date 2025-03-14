using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ExperimentServer.Shared;

public partial class EditExperimentDialog
{
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public ExperimentModel? Model { get; set; }

    [Parameter]
    public List<ExperimentModel>? Models { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private string? ErrorMessage;

    private bool IsSaving { get; set; }

    private async Task SaveAsync()
    {
        if (Model is null || Models is null) return;

        if (string.IsNullOrEmpty(Model.Name))
        {
            ErrorMessage = "please enter a valid name";
            return;
        }
        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        if (Model.Id == Guid.Empty)
        {
            Model.Id = Guid.NewGuid();
            Model.Created = DateTime.UtcNow;
        }

        Models = Models.Where(x => x.Id != Model.Id).ToList();
        Models.Add(Model);

        var response = await Client!.SaveExperimentsAsync(ProjectId, Models);
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
