using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class ShowImageDialog
{
    [Parameter]
    public string? Base64 { get; set; }
}
