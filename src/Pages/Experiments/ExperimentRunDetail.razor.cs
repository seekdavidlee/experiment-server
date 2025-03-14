using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentRunDetail
{
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [Parameter]
    public Guid RunId { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    private ExperimentRun? model;

    private List<ExperimentLog>? Logs { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentRun), out var modelObj))
        {
            model = (ExperimentRun)modelObj;

            Logs = await Client!.GetExperimentRunLogsAsync(ProjectId, ExperimentId, RunId);
        }
    }
}
