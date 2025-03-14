using ExperimentServer;
using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class GroundTruthReferenceDialog
{
    [Parameter]
    public GroundTruthReference? Details { get; set; }

    [Inject]
    private HttpClient? HttpClient { get; set; }

    private GroundTruth? model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var response = await HttpClient!.GetAsync($"ground-truths/item/{Details!.Id}");
        if (response.IsSuccessStatusCode)
        {
            model = await response.DeserializeResponse<GroundTruth>();
        }
    }
}
