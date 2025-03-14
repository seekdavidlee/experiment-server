using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ExperimentServer.Shared;

public partial class EditDataSetDialog
{
    [Parameter]
    public DataSetModel? Model { get; set; }

    [Parameter]
    public List<DataSetModel>? Models { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private string? ErrorMessage;

    private bool IsSaving { get; set; }

    private async Task SaveAsync()
    {
        if (Model is null || Models is null) return;

        if (string.IsNullOrEmpty(Model.DisplayName))
        {
            ErrorMessage = "please enter a valid display name";
            return;
        }
        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        if (Model.Id == Guid.Empty)
        {
            Model.Id = Guid.NewGuid();
        }

        Models = Models.Where(x => x.Id != Model.Id).ToList();
        Models.Add(Model);

        var response = await Client!.SaveDataSetsAsync(Models);
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
