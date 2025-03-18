using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;

namespace ExperimentServer.Pages.Datasets;

public partial class DataSetManager
{
    private RadzenDataGrid<GroundTruthImage>? dataGrid;

    private readonly DataSetManagerModel model = new();

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Parameter]
    public Guid DatasetId { get; set; }

    private DataSetModel? DataSetModel { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(DataSetModel), out var dataSet))
        {
            DataSetModel = (DataSetModel)dataSet;
        }
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetGroundTruthImagesAsync(DatasetId!));
        await dataGrid!.Reload();
    }

    private void AddNew()
    {
        var groundTruthImage = new GroundTruthImage
        {
            Id = Guid.NewGuid(),
            Fields = JsonSerializer.Deserialize<DataSetModelField[]>(JsonSerializer.Serialize(DataSetModel!.Fields)),   // create a copy of all valid fields
        };
        UserSession!.Items[nameof(GroundTruthImage)] = groundTruthImage;
        NavigationManager!.NavigateTo($"datasets/{DatasetId}/images/ground-truth/{groundTruthImage.Id}");
    }

    private void Edit(GroundTruthImage groundTruthImage)
    {
        UserSession!.Items[nameof(GroundTruthImage)] = groundTruthImage;
        NavigationManager!.NavigateTo($"datasets/{DatasetId}/images/ground-truth/{groundTruthImage.Id}");
    }

    private void Back()
    {
        NavigationManager!.NavigateTo("/datasets");
    }

    private async Task CopyTo(GroundTruthImage groundTruthImage)
    {
        await DialogService!.OpenAsync<CopyGroundTruthToDatasetDialog>("Copy Ground Truth", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", groundTruthImage },
             { "Client", Client! },
             { "ExcludeDatasetId", DatasetId },
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "500px",
            Height = "300px",
        });
    }

    private async Task DeleteAsync(GroundTruthImage groundTruthImage)
    {
        ErrorMessage = null;
        var result = await DialogService!.Confirm($"Are you sure you want to delete <b>{groundTruthImage.DisplayName}</b>?", "Delete ground truth", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
        if (result is not null && result == true)
        {
            var response = await Client!.DeleteGroundTruthImageAsync(DatasetId, groundTruthImage);
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
}
