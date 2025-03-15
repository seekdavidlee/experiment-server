using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class TableView
{
    [Parameter]
    public string? Title { get; set; }
    [Parameter]
    public CompareTableModel? TableModel { get; set; }
}
