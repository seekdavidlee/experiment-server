using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Text.Json;

namespace ExperimentServer.Shared;

public partial class CopyGroundTruthToDatasetDialog
{
    [Parameter]
    public GroundTruthImage? Model { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private List<DataSetModel>? Datasets { get; set; }

    [Parameter]
    public Guid ExcludeDatasetId { get; set; }

    private DataSetModel? SelectedDataset { get; set; }

    private string? ErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        Datasets = (await Client!.GetDataSetsAsync()).Where(x => x.Id != ExcludeDatasetId).ToList();
    }

    public async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedDataset is null)
        {
            ErrorMessage = "Please select a dataset.";
            return;
        }

        var imgResponse = await Client!.GetGroundTruthImageAsync(SelectedDataset.Id, Model!);
        if (!imgResponse.Success)
        {
            ErrorMessage = imgResponse.ErrorMessage;
            return;
        }

        var copy = JsonSerializer.Deserialize<GroundTruthImage>(JsonSerializer.Serialize(Model))!;
        copy.Id = Guid.NewGuid();

        List<GroundTruthTag> copyTags = [new GroundTruthTag { Name = "image_dataset_source", Value = $"Dataset: {Model!.DisplayName}" }];
        if (copy.Tags is null)
        {
            copy.Tags = [..copyTags];
        }
        else
        {
            var list = copy.Tags.ToList();
            list.AddRange(copyTags);
            copy.Tags = copyTags.ToArray();
        }

        await Client!.SaveGroundTruthImageAsync(SelectedDataset.Id, copy, imgResponse.Result);

        DialogService!.Close();
    }

    public void Cancel()
    {
        DialogService!.Close();
    }
}
