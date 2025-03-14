using Eklee.Reports.Client.Models;
using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ExperimentServer.Pages.GroundTruths;

public partial class GroundTruthsDashboard
{
    private RadzenDataGrid<GroundTruthReference>? dataGrid;

    private readonly GroundTruthsDashboardModel model = new(Constants.ImageInferenceTypes.Receipts);

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
        await Task.Delay(500);
        var items = await Client!.GetGroundTruthReferencesAsync();
        model.GroundTruthReferences.Clear();
        model.GroundTruthReferences = items;
        dataGrid?.Reload();

        StateHasChanged();
    }

    private void EnableAdd()
    {
        model.AddNewGroundTruth = true;
    }

    private void CancelAdd()
    {
        model.AddNewGroundTruth = false;
        StateHasChanged();
    }

    private async Task OnSaveAsync(GroundTruth groundTruth)
    {
        model.AddNewGroundTruth = false;
        StateHasChanged();

        model.IsSavingGroundTruth = true;

        await Client!.SaveGroundTruthAsync(groundTruth);
        model.IsSavingGroundTruth = false;
        StateHasChanged();

        await RefreshAsync();
    }

    private async Task ShowDetails(GroundTruthReference groundTruthReference)
    {
        await DialogService!.OpenAsync<GroundTruthReferenceDialog>(groundTruthReference.Id, new Dictionary<string, object> { { "Details", groundTruthReference } },
             new DialogOptions
             {
                 CloseDialogOnEsc = true,
                 Width = "700px",
                 Height = "512px",
             });
    }
}
