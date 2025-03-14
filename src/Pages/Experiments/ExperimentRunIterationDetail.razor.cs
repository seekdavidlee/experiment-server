using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentRunIterationDetail
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

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    private ExperimentRunResult? experimentRunResult;
    private ExperimentMetric? experimentMetric;
    private string? Base64Image { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentRunResult), out var experimentRunResultObj) && experimentRunResultObj is not null &&
            UserSession.Items.TryGetValue(nameof(ExperimentMetric), out var experimentMetricObj) && experimentMetricObj is not null)
        {
            experimentRunResult = (ExperimentRunResult)experimentRunResultObj;
            experimentMetric = (ExperimentMetric)experimentMetricObj;

            if (experimentMetric.Meta!.TryGetValue("image_file_path", out var imagePathObj))
            {
                var getImageResponse = await Client!.GetImageAsync(imagePathObj);
                if (getImageResponse.Success)
                {
                    Base64Image = $"data:image/jpg;base64,{Convert.ToBase64String(getImageResponse.Result)}";
                }
            }
        }
        else
        {

            NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{RunId}");
        }
    }
}
