using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Text.Json;

namespace ExperimentServer.Shared;

public partial class CopyAllDatasetGroundTruthsToDialog
{
    [Parameter]
    public DataSetModel? Model { get; set; }

    [Parameter]
    public List<GroundTruthImage>? Models { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private List<DataSetModel>? Datasets { get; set; }

    [Parameter]
    public Guid ExcludeDatasetId { get; set; }

    private DataSetModel? SelectedDataset { get; set; }

    private string? ErrorMessage;

    private int CopiedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }

    private bool ShowResults { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Datasets = (await Client!.GetDataSetsAsync()).Where(x => x.Id != ExcludeDatasetId).ToList();
    }

    private double Progress { get; set; }

    public async Task StartCopyAsync()
    {
        ErrorMessage = null;
        if (SelectedDataset is null)
        {
            ErrorMessage = "Please select a dataset.";
            return;
        }

        for (var i = 0; i < Models!.Count; i++)
        {
            var gt = Models[i];

            var imgResponse = await Client!.GetGroundTruthImageAsync(ExcludeDatasetId, gt);
            if (!imgResponse.Success)
            {
                ErrorCount++;
                continue;
            }

            var existing = await Client!.GetGroundTruthImagesAsync(SelectedDataset.Id);
            if (!existing.Success)
            {
                ErrorCount++;
                continue;
            }

            if (existing.Result.Any(x => string.Compare(x.DisplayName, gt!.DisplayName, true) == 0))
            {
                SkippedCount++;
                continue;
            }

            var copy = JsonSerializer.Deserialize<GroundTruthImage>(JsonSerializer.Serialize(gt))!;
            copy.Id = Guid.NewGuid();

            List<GroundTruthTag> copyTags = [new GroundTruthTag { Name = "image_dataset_source", Value = $"Dataset: {gt!.DisplayName}" }];
            if (copy.Tags is null)
            {
                copy.Tags = [.. copyTags];
            }
            else
            {
                var list = copy.Tags.ToList();
                list.AddRange(copyTags);
                copy.Tags = [.. list];
            }

            var res = await Client!.SaveGroundTruthImageAsync(SelectedDataset.Id, copy, imgResponse.Result);
            if (!res.Success)
            {
                ErrorCount++;
                continue;
            }
            else
            {
                CopiedCount++;
            }

            Progress = ((i + 1) / Models.Count) * 100F;

            StateHasChanged();
        }

        ShowResults = true;
    }

    public void Cancel()
    {
        DialogService!.Close();
    }
}
