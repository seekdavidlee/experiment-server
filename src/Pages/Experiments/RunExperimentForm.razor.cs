using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class RunExperimentForm
{
    private readonly ExperimentRun model = new()
    {
        Iterations = 1,
        MaxTokens = 2048,
    };

    private string? ErrorMessage;

    [Inject]
    public InferenceApi? InferenceClient { get; set; }

    [Inject]
    public FileSystemApi? Client { get; set; }

    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    private bool IsSaving { get; set; }

    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    private List<DataSetModel>? DataSets { get; set; }

    private Guid SelectedDataSetId { get; set; }

    private List<string>? ModelIds { get; set; }

    private Prompts? OriginalPrompts;
    private string? GroundTruthTagFilters { get; set; }

    protected override async Task OnInitializedAsync()
    {
        DataSets = await Client!.GetDataSetsAsync();
        var response = await InferenceClient!.GetModelsAsync();
        if (response.Success)
        {
            ModelIds = [.. response.Result];

            var promptResponse = await Client.GetPromptsAsync(ProjectId, ExperimentId);
            if (promptResponse.Success)
            {
                model.SystemPrompt = promptResponse.Result!.System;
                model.UserPrompt = promptResponse.Result.User;

                OriginalPrompts = new Prompts(model.SystemPrompt, model.UserPrompt);
            }
            else
            {
                ErrorMessage = promptResponse.ErrorMessage;
            }
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

        if (model.Iterations < 1)
        {
            ErrorMessage = "please enter a select a valid iteration";
            return;
        }

        if (SelectedDataSetId == Guid.Empty)
        {
            ErrorMessage = "please select a data set";
            return;
        }

        ErrorMessage = null;
        IsSaving = true;
        StateHasChanged();

        if (GroundTruthTagFilters is not null)
        {
            var parts = GroundTruthTagFilters.Split(';');
            model.GroundTruthTagFilters = parts.Where(x => x.Split('=').Length == 2).Select(x =>
            {
                var parts = x.Split('=');
                return new ExperimentRunGroundTruthTagFilter { Name = parts[0], Value = parts[1] };
            }).ToArray();
        }

        model.ExperimentId = ExperimentId;
        model.ProjectId = ProjectId;
        model.DataSetFileSystemApiPath = Client!.GetDataSetPath(SelectedDataSetId);
        model.OutputFileSystemApiPath = Client!.GetExperimentRunsPath(ProjectId, ExperimentId, false);

        var response = await InferenceClient!.RunExperimentAsync(model);
        IsSaving = false;

        if (!response.Success)
        {
            ErrorMessage = response.ErrorMessage;
            StateHasChanged();
            return;
        }

        if (OriginalPrompts is not null && (OriginalPrompts.System != model.SystemPrompt || OriginalPrompts.User != model.UserPrompt))
        {
            await Client.SavePromptsAsync(ProjectId, ExperimentId, new Prompts(model.SystemPrompt, model.UserPrompt ?? ""));
        }

        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");

    }

    private void Cancel()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
    }
}
