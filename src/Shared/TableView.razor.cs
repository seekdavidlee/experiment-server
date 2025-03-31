using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class TableView
{
    [Parameter]
    public CompareTableModel? TableModel { get; set; }

    [Parameter]
    public Action<CompareTableCell>? OnButtonClicked { get; set; }

    private void ButtonClicked(CompareTableCell compareTableCell)
    {
        if (OnButtonClicked is not null)
        {
            OnButtonClicked(compareTableCell);
        }
    }
}
