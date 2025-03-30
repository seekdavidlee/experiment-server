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

    [Inject]
    private ILogger<DataSetManager>? Logger { get; set; }

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
        else
        {
            DataSetModel = (await Client!.GetDataSetsAsync()).SingleOrDefault(x => x.Id == DatasetId);
            if (DataSetModel is null)
            {
                ErrorMessage = $"Dataset {DatasetId} is not valid.";
                return;
            }
        }

        if (DataSetModel.Fields is not null)
        {
            KeyFilterFields = DataSetModel.Fields!.Select(x => x.Name!).ToArray();
        }

        await RefreshAsync();
    }

    private bool IsReady { get; set; }

    private async Task RefreshAsync()
    {
        IsReady = false;
        model.Reset();
        var response = await Client!.GetGroundTruthImagesAsync(DatasetId!);
        if (!response.Success)
        {
            ErrorMessage = response.ErrorMessage;
            return;
        }

        bool isDirty = false;
        if (DataSetModel is not null && DataSetModel.Fields is not null)
        {
            // sync ground truth fields to latest version of dataset fields
            foreach (var gt in response.Result)
            {
                if (gt.Fields is null) continue;

                bool gtUpdated = false;
                foreach (var field in gt.Fields)
                {
                    var dsField = DataSetModel.Fields.SingleOrDefault(x => x.Name == field.Name);
                    if (dsField is not null)
                    {
                        if (dsField.Expression != field.Expression)
                        {
                            field.Expression = dsField.Expression;
                            gtUpdated = true;
                        }

                        if (dsField.IsSubjective != field.IsSubjective)
                        {
                            field.IsSubjective = dsField.IsSubjective;
                            gtUpdated = true;
                        }
                    }
                }

                if (gtUpdated)
                {
                    var res = await Client.SaveGroundTruthImageAsync(DatasetId, gt);
                    if (res.Success)
                    {
                        Logger!.LogInformation("Ground truth image {GroundTruthDisplayName} updated to match latest version of dataset fields.", gt.DisplayName);
                        isDirty = true;
                    }
                }
            }
        }

        model.Update(response.Result);
        if (dataGrid is not null)
        {
            await dataGrid.Reload();
        }

        if (isDirty)
        {
            await DialogService!.Alert("Meta data of some fields have been updated to match the latest version of the dataset.");
        }

        IsReady = true;
    }

    private void AddNew()
    {
        var groundTruthImage = new GroundTruthImage
        {
            Id = Guid.NewGuid(),
            Fields = JsonSerializer.Deserialize<DataSetModelField[]>(JsonSerializer.Serialize(DataSetModel!.Fields)),   // create a copy of all valid fields
        };

        UserSession!.Items[$"ListOf{nameof(GroundTruthImage)}"] = model.Items;
        UserSession.Items[nameof(GroundTruthImage)] = groundTruthImage;
        NavigationManager!.NavigateTo($"datasets/{DatasetId}/images/ground-truth/{groundTruthImage.Id}?action=new");
    }

    private void Edit(GroundTruthImage groundTruthImage)
    {
        UserSession!.Items[$"ListOf{nameof(GroundTruthImage)}"] = model.Items;
        UserSession.Items[nameof(GroundTruthImage)] = groundTruthImage;
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
            Height = "350px",
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

    private async Task OpenCopyAllToDialog()
    {
        await DialogService!.OpenAsync<CopyAllDatasetGroundTruthsToDialog>("Copy Ground Truth", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", DataSetModel! },
             { "Models", model.Items },
             { "Client", Client! },
             { "ExcludeDatasetId", DatasetId },
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "500px",
            Height = "350px",
        });
    }

    private string[]? KeyFilterFields { get; set; }
    private async void OnFiltersChanged(List<FilterPanelModel> filters)
    {
        model.ApplyFilters(filters);
        if (dataGrid is not null)
        {
            await dataGrid.Reload();
        }
    }
}
