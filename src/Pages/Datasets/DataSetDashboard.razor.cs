using Eklee.Reports.Client.Models;
using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ExperimentServer.Pages.Datasets;

public partial class DataSetDashboard
{
    private string? ErrorMessage { get; set; }
    private RadzenDataGrid<DataSetModel>? dataGrid;

    private readonly DataSetDashboardModel model = new();
    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetDataSetsAsync());
        await dataGrid!.Reload();
    }

    private void OpenDataset(DataSetModel datasetModel)
    {
        var path = $"datasets/{datasetModel.Id}";
        UserSession!.Items[nameof(DataSetModel)] = datasetModel;
        NavigationManager!.NavigateTo(path);
    }

    private async Task Edit(DataSetModel datasetModel)
    {
        bool? result = await DialogService!.OpenAsync<EditDataSetDialog>("Edit DataSet", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", datasetModel },
             { "Models", model.Items },
             { "Client", Client! }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "700px",
        });

        if (result == true)
        {
            await UpdateGroundTruthImageFields(datasetModel);
            await RefreshAsync();
        }
    }

    private async Task AddNew()
    {
        bool? result = await DialogService!.OpenAsync<EditDataSetDialog>("New DataSet", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", new DataSetModel{ Fields=[] } },
             { "Models", model.Items },
             { "Client", Client! }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "700px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }

    private async Task UpdateGroundTruthImageFields(DataSetModel dataSetModel)
    {
        if (dataSetModel.Fields is null || dataSetModel.Fields.Length == 0)
        {
            return;
        }

        var groundTruths = await Client!.GetGroundTruthImagesAsync(dataSetModel.Id);
        foreach (var groundTruth in groundTruths)
        {
            bool isDirty = false;

            List<DataSetModelField> missing = [];
            foreach (var dsField in dataSetModel.Fields)
            {
                if (groundTruth.Fields is null)
                {
                    continue;
                }

                var gtField = groundTruth.Fields.SingleOrDefault(x => x.Name == dsField.Name);
                if (gtField is not null)
                {
                    if (gtField.Expression != dsField.Expression)
                    {
                        gtField.Expression = dsField.Expression;
                        isDirty = true;
                    }

                    if (gtField.IsSubjective != dsField.IsSubjective)
                    {
                        gtField.IsSubjective = dsField.IsSubjective;
                        isDirty = true;
                    }
                }
                else
                {
                    missing.Add(dsField.Clone());
                }
            }

            if (missing.Count > 0)
            {
                var list = groundTruth.Fields!.ToList();
                list.AddRange(missing);
                groundTruth.Fields = [.. list];
                isDirty = true;
            }

            if (isDirty)
            {
                var response = await Client.SaveGroundTruthImageAsync(dataSetModel.Id, groundTruth);
                if (!response.Success)
                {
                    await DialogService!.Alert(response.ErrorMessage, "Unable to update ground gruth");
                }
            }
        }
    }

    private async Task DeleteAsync(DataSetModel dataSetModel)
    {
        ErrorMessage = null;
        var result = await DialogService!.Confirm($"Are you sure you want to delete {dataSetModel.DisplayName}?", "Delete dataset", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
        if (result is not null && result == true)
        {
            var updateMetaResponse = await Client!.SaveDataSetsAsync(model!.Items.Where(x => x.Id != dataSetModel.Id).ToList());
            if (updateMetaResponse.Success)
            {
                var response = await Client!.DeleteDatasetAsync(dataSetModel.Id);
                if (response.Success)
                {
                    await RefreshAsync();
                }
                else
                {
                    ErrorMessage = response.ErrorMessage;
                }
            }
            else
            {
                ErrorMessage = updateMetaResponse.ErrorMessage;
            }
        }
    }

    private void Back()
    {
        NavigationManager!.NavigateTo("/");
    }
}
