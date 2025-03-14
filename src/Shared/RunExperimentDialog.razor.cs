using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ExperimentServer.Shared;

public partial class RunExperimentDialog
{
    private readonly ExperimentRun model = new()
    {
        Iterations = 1,
        MaxTokens = 2048,
        SystemPrompt = "You are an AI assistant",
        UserPrompt = "Tell me about the picture"
    };

    private string? ErrorMessage;

    [Inject]
    public InferenceApi? InferenceClient { get; set; }

    [Parameter]
    public FileSystemApi? Client { get; set; }

    private bool IsSaving { get; set; }

    [Parameter]
    public DialogService? DialogService { get; set; }

    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    private List<DataSetModel>? dataSets { get; set; }

    private Guid selectedDataSetId { get; set; }

    private List<string>? modelIds { get; set; }

    protected override async Task OnInitializedAsync()
    {
        dataSets = await Client!.GetDataSetsAsync();
        var response = await InferenceClient!.GetModelsAsync();
        if (response.Success)
        {
            modelIds = [.. response.Result];
        }
        else
        {
            ErrorMessage = response.ErrorMessage;
        }
    }

    private async Task SaveAsync()
    {

        if (string.IsNullOrEmpty(model.SystemPrompt))
        {
            ErrorMessage = "please enter a valid system prompt";
            return;
        }

        if (string.IsNullOrEmpty(model.UserPrompt))
        {
            ErrorMessage = "please enter a valid user prompt";
            return;
        }

        if (model.Iterations < 1)
        {
            ErrorMessage = "please enter a select a valid iteration";
            return;
        }

        if (selectedDataSetId == Guid.Empty)
        {
            ErrorMessage = "please select a data set";
            return;
        }

        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        model.ExperimentId = ExperimentId;
        model.ProjectId = ProjectId;
        model.DataSetFileSystemApiPath = Client!.GetDataSetPath(selectedDataSetId);
        model.OutputFileSystemApiPath = Client!.GetExperimentRunsPath(ProjectId, ExperimentId, false);

        var response = await InferenceClient!.RunExperimentAsync(model);
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
