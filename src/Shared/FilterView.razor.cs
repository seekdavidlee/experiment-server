using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ExperimentServer.Shared;

public partial class FilterView
{
    private bool IsEditMode { get; set; } = true;

    [Parameter]
    public GroundTruthTagModel? Value { get; set; }

    [Parameter]
    public Action<GroundTruthTagModel>? OnDelete { get; set; }

    [Parameter]
    public Action? OnSave { get; set; }

    [Inject]
    public DialogService? DialogService { get; set; }

    private readonly IEnumerable<string> comparisons = Enum.GetValues(typeof(GroundTruthTagComprisons)).Cast<GroundTruthTagComprisons>().Select(x => x.ToString());

    private string? selectedComparison;

    private void Save()
    {
        if (string.IsNullOrEmpty(Value!.Tag.Name))
        {
            DialogService!.Alert("Tag name is required", "Error");
            return;
        }

        if (string.IsNullOrEmpty(Value!.Tag.Value))
        {
            DialogService!.Alert("Tag value is required", "Error");
            return;
        }

        if (string.IsNullOrEmpty(selectedComparison))
        {
            DialogService!.Alert("Comparison is required", "Error");
            return;
        }

        Value.Comprison = Enum.Parse<GroundTruthTagComprisons>(selectedComparison!);

        IsEditMode = false;
        OnSave!();
    }

    private void Close()
    {
        IsEditMode = false;
    }

    private void Delete()
    {
        OnDelete!(Value!);
    }

    private void Open()
    {
        IsEditMode = true;
    }
}
