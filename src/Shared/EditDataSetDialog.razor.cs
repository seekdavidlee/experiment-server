using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Text.Json;

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

    private DataSetModel? WorkingCopy;
    protected override void OnInitialized()
    {
        if (Model is not null)
        {
            WorkingCopy = JsonSerializer.Deserialize<DataSetModel>(JsonSerializer.Serialize(Model));
        }
    }

    private string[] validExpressions = ["string", "money"];

    private async Task SaveAsync()
    {
        if (WorkingCopy is null || WorkingCopy is null) return;

        if (string.IsNullOrEmpty(WorkingCopy.DisplayName))
        {
            ErrorMessage = "please enter a valid display name";
            return;
        }

        if (WorkingCopy.Fields is null || WorkingCopy.Fields.Length == 0)
        {
            ErrorMessage = "please enter at least one field";
            return;
        }

        foreach (var field in WorkingCopy.Fields)
        {
            if (string.IsNullOrEmpty(field.Name) || string.IsNullOrEmpty(field.Expression))
            {
                ErrorMessage = "please enter a valid field name and expression";
                return;
            }

            if (!validExpressions.Contains(field.Expression.ToLower()))
            {
                var all = string.Join(',', validExpressions);
                ErrorMessage = $"invalid expression, must be one of the following: {all}";
                return;
            }
        }

        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        if (WorkingCopy.Id == Guid.Empty)
        {
            WorkingCopy.Id = Guid.NewGuid();
        }

        Models = Models!.Where(x => x.Id != WorkingCopy.Id).ToList();
        Models.Add(WorkingCopy);

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

    private void NewField()
    {
        List<DataSetModelField> fields = WorkingCopy!.Fields is null ? [] : [.. WorkingCopy.Fields];
        fields.Add(new DataSetModelField());
        WorkingCopy.Fields = [.. fields];
    }

    private void RemoveField(DataSetModelField field)
    {
        WorkingCopy!.Fields = WorkingCopy.Fields!.Where(x => x != field).ToArray();
    }
}
