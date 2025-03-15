
using ExperimentServer.Models;
using ExperimentServer.Services;
using FastMember;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Pages.Experiments;

public partial class ExperimentsComparison
{
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    private CompareTableModel? Inputs;

    protected override Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentsComparison), out var experimentRunsObj))
        {
            var accessor = TypeAccessor.Create(typeof(ExperimentRun));
            var members = accessor.GetMembers();
            List<string> props = [
                nameof(ExperimentRun.ModelId),
                nameof(ExperimentRun.SystemPrompt),
                nameof(ExperimentRun.UserPrompt),
                nameof(ExperimentRun.Temperature),
                nameof(ExperimentRun.TopP),
                ];

            List<string> colNames = [];
            Dictionary<string, CompareTableRow> rows = [];
            foreach (var experimentRun in (List<ExperimentRun>)experimentRunsObj)
            {
                colNames.Add(experimentRun.Id.ToString());
                foreach (var p in props)
                {
                    if (!rows.TryGetValue(p, out var row))
                    {
                        row = new CompareTableRow { Name = p, Cells = [] };
                        rows.Add(p, row);
                    }

                    row.Cells!.Add(new CompareTableCell { Value = accessor[experimentRun, p].ToString() });
                }
            }

            Inputs = new CompareTableModel { ColumnNames = [.. colNames], Rows = [.. rows.Values] };
        }
        return base.OnInitializedAsync();
    }

    private void Back()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
    }
}