
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

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private ILogger<ExperimentsComparison>? Logger { get; set; }

    private CompareTableModel? Inputs;
    private CompareTableModel? FieldsAccuracy;
    private readonly List<CompareTableModel> PerRunFailureComparisions = [];

    protected override async Task OnInitializedAsync()
    {
        if (UserSession!.Items.TryGetValue(nameof(ExperimentsComparison), out var experimentRunsObj))
        {
            var runs = (List<ExperimentRun>)experimentRunsObj;

            InitInputs(runs);
            await InitAsync(runs);
        }
    }

    private void InitInputs(List<ExperimentRun> runs)
    {
        var accessor = TypeAccessor.Create(typeof(ExperimentRun));
        List<string> props = [
            nameof(ExperimentRun.ModelId),
                nameof(ExperimentRun.SystemPrompt),
                nameof(ExperimentRun.UserPrompt),
                nameof(ExperimentRun.Temperature),
                nameof(ExperimentRun.TopP),
                nameof(ExperimentRun.Iterations),
                ];

        List<string> colNames = [];
        Dictionary<string, CompareTableRow> rows = [];
        foreach (var experimentRun in runs)
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

        Inputs = new CompareTableModel { ColumnNames = [.. colNames.Select(x => new CompareTableColumn { Name = x })], Rows = [.. rows.Values] };
    }

    private async Task InitAsync(List<ExperimentRun> runs)
    {
        List<string> fieldAccuracyColNames = [];
        Evaluation eval = new();
        Dictionary<string, CompareTableRow> fieldAccuracyRows = [];
        foreach (var experimentRun in runs)
        {
            CompareTableModel perRunFailureComparision = new()
            {
                Title = $"{experimentRun.Id} failures",
                Rows = [],
                ColumnNames = [
                    new CompareTableColumn { Name = "Field" },
                    new CompareTableColumn { Name = "Expected" },
                    new CompareTableColumn { Name = "Actual" },
                    new CompareTableColumn { Name = "Message" }
                ]
            };

            fieldAccuracyColNames.Add(experimentRun.Id.ToString());
            Dictionary<string, FieldAccuracy> fields = [];

            var results = await Client!.GetExperimentRunResultsAsync(ProjectId, ExperimentId, experimentRun.Id);
            if (results is null)
            {
                Logger!.LogError("unable to get experiment run");
                return;
            }

            var metrics = await Client!.GetExperimentRunMetricsAsync(ProjectId, ExperimentId, experimentRun.Id);
            if (metrics is null)
            {
                Logger!.LogError("unable to get experiment run metrics");
                return;
            }

            foreach (var res in results)
            {
                var metric = metrics.SingleOrDefault(x => x.ResultId == res.Id);
                if (metric is null)
                {
                    Logger!.LogError("unable to get metric for result");
                    return;
                }

                if (!metric.Meta!.TryGetValue("image_file_path", out var imagePathObj))
                {
                    Logger!.LogError("unable to get image file path");
                    return;
                }

                var groundTruthResponse = await Client.GetJsonAsync<GroundTruthImage>(imagePathObj.Replace(".jpg", ".json"));
                if (!groundTruthResponse.Success)
                {
                    Logger!.LogError("unable to get ground truth json");
                    return;
                }

                foreach (var assertion in eval.GetAssertions(groundTruthResponse.Result!, res.Text!))
                {
                    if (!fields.TryGetValue(assertion.Field, out var fieldAcc))
                    {
                        fieldAcc = new FieldAccuracy { Name = assertion.Field };
                        fields.Add(assertion.Field, fieldAcc);
                    }

                    fieldAcc.Total++;
                    if (assertion.Success)
                    {
                        fieldAcc.Correct++;
                    }
                    else
                    {
                        string key = $"{groundTruthResponse.Result!.DisplayName}.{assertion.Field}";
                        var existingPerFieldFailureComparisionRow = perRunFailureComparision.Rows!.SingleOrDefault(x => x.Name == key);
                        if (existingPerFieldFailureComparisionRow is null)
                        {
                            var newPerFieldFailureComparisionRow = new CompareTableRow { Name = key, Cells = [] };
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = assertion.Field });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = assertion.Expected });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Values = [assertion.Actual ?? ""] });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Values = [assertion.Message ?? ""] });
                            perRunFailureComparision.Rows!.Add(newPerFieldFailureComparisionRow);
                        }
                        else
                        {
                            var actualCell = existingPerFieldFailureComparisionRow.Cells![2];
                            if (assertion.Actual is not null && !actualCell.Values!.Contains(assertion.Actual))
                            {
                                actualCell.Values = [.. actualCell.Values!, assertion.Actual];
                            }

                            var messageCell = existingPerFieldFailureComparisionRow.Cells![3];
                            if (assertion.Message is not null && !messageCell.Values!.Contains(assertion.Message))
                            {
                                messageCell.Values = [.. messageCell.Values!, assertion.Message];
                            }
                        }
                    }
                }
            }

            foreach (var field in fields.Values)
            {
                if (!fieldAccuracyRows.TryGetValue(field.Name!, out var fieldAccuracyRow))
                {
                    fieldAccuracyRow = new CompareTableRow { Name = field.Name, Cells = [] };
                    fieldAccuracyRows.Add(field.Name!, fieldAccuracyRow);
                }
                decimal accuracy = field.Total == 0 ? 0 : (decimal)field.Correct / field.Total;
                fieldAccuracyRow.Cells!.Add(new CompareTableCell { Value = String.Format("{0:P3}", accuracy) });
            }

            if (perRunFailureComparision.Rows!.Count != 0)
            {
                // cleanup row name
                foreach(var row in perRunFailureComparision.Rows!)
                {
                    var i = row.Name!.LastIndexOf('.');
                    row.Name = row.Name[..i];
                }
                PerRunFailureComparisions.Add(perRunFailureComparision);
            }
        }

        FieldsAccuracy = new CompareTableModel
        {
            ColumnNames = [.. fieldAccuracyColNames.Select(x => new CompareTableColumn
            {
                Name = x,
                HyperLink = $"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{x}"
            })],
            Rows = [.. fieldAccuracyRows.Values]
        };
    }

    private void Back()
    {
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs");
    }
}

public class FieldAccuracy
{
    public string? Name { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }
}