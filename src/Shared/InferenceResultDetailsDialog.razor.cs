using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class InferenceResultDetailsDialog
{
    [Parameter]
    public ImageInferenceResult? Model { get; set; }
}
