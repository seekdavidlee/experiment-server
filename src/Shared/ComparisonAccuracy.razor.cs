
using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace ExperimentServer.Shared;

public partial class ComparisonAccuracy
{
    [Parameter]
    public List<ExperimentRun>? Runs { get; set; }

    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ExperimentId { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private ILogger<ComparisonAccuracy>? Logger { get; set; }

    [Inject]
    private InProgressIndicatorService? InProgressIndicator { get; set; }

    private CompareTableModel? FieldsAccuracy;
    private readonly List<CompareTableModel> PerRunFailureComparisions = [];
    private readonly List<ExpRunModel> ExpRuns = [];
    private readonly HashSet<string> fieldKeys = [];
    private bool isInitialized;
    private bool toggleMessageColumn;

    protected override async Task OnInitializedAsync()
    {
        InProgressIndicator!.Show("loading experiments");
        await InitAsync(Runs!);
        InProgressIndicator!.Hide();
    }

    private async Task InitAsync(List<ExperimentRun> runs)
    {
        List<(string, Guid)> fieldAccuracyColNames = [];
        Evaluation eval = new();

        foreach (var experimentRun in runs)
        {
            CompareTableModel perRunFailureComparision = new()
            {
                Title = $"{experimentRun.GetIdOrDescription()} - Field level failures",
                Rows = [],
                ColumnNames = [
                    new CompareTableColumn { Name = "Field" },
                    new CompareTableColumn { Name = "Expected" },
                    new CompareTableColumn { Name = "Actual" },
                    new CompareTableColumn { Name = "Message", Hide = true },
                    new CompareTableColumn { Name = "Tags" },
                    new CompareTableColumn { Name = "Actions" }
                ]
            };
            PerRunFailureComparisions.Add(perRunFailureComparision);

            fieldAccuracyColNames.Add((experimentRun.GetIdOrDescription(), experimentRun.Id));

            var item = new ExpRunModel(Client!, ProjectId, ExperimentId, experimentRun, Logger!);
            await item.InitAsync(eval);
            ExpRuns.Add(item);
        }


        foreach (var e in ExpRuns)
        {
            foreach (var r in e.Results)
            {
                if (r.GroundTruth.Tags is not null)
                {
                    foreach (var t in r.GroundTruth.Tags)
                    {
                        if (t.Name is not null) fieldKeys.Add(t.Name);
                    }
                }
            }
        }

        FieldsAccuracy = new CompareTableModel
        {
            Title = "Fields Accuracy",
            ColumnNames = [.. fieldAccuracyColNames.Select(x => new CompareTableColumn
            {
                Name = x.Item1,
                HyperLink = $"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{x.Item2}"
            })],
            Rows = []
        };

        Refresh([]);
        RefreshFailures([]);

        isInitialized = true;
    }

    private void ShowMessageColumn(bool show)
    {
        foreach (var perRunFailureComparision in PerRunFailureComparisions)
        {
            perRunFailureComparision.ColumnNames!.Single(x => x.Name == "Message").Hide = !show;
        }
    }

    private void RefreshFailures(List<FilterPanelModel> filters)
    {
        for (var i = 0; i < PerRunFailureComparisions.Count; i++)
        {
            var perRunFailureComparision = PerRunFailureComparisions[i];
            var item = ExpRuns[i];

            perRunFailureComparision.Rows!.Clear();

            foreach (var res in item.Results)
            {
                if (ShouldFilter(filters, res))
                {
                    continue;
                }

                foreach (var assertion in res.Assertions)
                {
                    if (!assertion.Success)
                    {
                        string key = $"{res.GroundTruth.DisplayName}.{assertion.Field}";
                        var existingPerFieldFailureComparisionRow = perRunFailureComparision.Rows!.SingleOrDefault(x => x.Name == key);
                        if (existingPerFieldFailureComparisionRow is null)
                        {
                            var data = new Dictionary<string, object>
                            {
                                {"RunId", item.Id },
                                { nameof(ExperimentRunResult), res.RunResult },
                                { nameof(ExperimentMetric), res.Metric }
                            };

                            var newPerFieldFailureComparisionRow = new CompareTableRow { Name = key, Cells = [] };
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = assertion.Field });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = assertion.Expected });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Values = [assertion.Actual ?? ""] });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Values = [assertion.Message ?? ""] });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = res.GroundTruth.Tags.GetString() });
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = "View", IsButton = true, Data = data });
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

            if (perRunFailureComparision.Rows!.Count != 0)
            {
                // cleanup row name
                foreach (var row in perRunFailureComparision.Rows!)
                {
                    row.Name = row.Name![..row.Name!.LastIndexOf('.')];
                }
            }
        }
    }

    private bool ShouldFilter(List<FilterPanelModel> filters, ExperimentRunResultModel res)
    {
        if (filters.Count > 0 &&
            res.GroundTruth.Tags is not null &&
            !filters.All(x => res.GroundTruth.Tags.Any(y => x.Name == y.Name &&
                (x.Comprison == FilterPanelModelComprisons.Equal ? x.Value == y.Value : x.Value != y.Value))))
        {
            Logger!.LogInformation("skipping result due to filter");
            return true;
        }
        return false;
    }

    private void Refresh(List<FilterPanelModel> filters)
    {
        Dictionary<string, CompareTableRow> fieldAccuracyRows = [];

        foreach (var item in ExpRuns)
        {
            Dictionary<string, FieldAccuracy> fields = [];
            foreach (var res in item.Results)
            {
                if (ShouldFilter(filters, res))
                {
                    continue;
                }

                foreach (var assertion in res.Assertions)
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
                }
            }

            int totalFieldsCorrect = 0;
            int totalFields = 0;
            foreach (var field in fields.Values)
            {
                if (!fieldAccuracyRows.TryGetValue(field.Name!, out var fieldAccuracyRow))
                {
                    fieldAccuracyRow = new CompareTableRow { Name = field.Name, Cells = [] };
                    fieldAccuracyRows.Add(field.Name!, fieldAccuracyRow);
                }
                decimal accuracy = field.Total == 0 ? 0 : (decimal)field.Correct / field.Total;
                totalFieldsCorrect += field.Correct;
                totalFields += field.Total;
                fieldAccuracyRow.Cells!.Add(new CompareTableCell { Value = String.Format("{0:P3}", accuracy) });
            }
            decimal totalAccuracy = totalFields == 0 ? 0 : (decimal)totalFieldsCorrect / totalFields;
            var overallCell = new CompareTableCell { Value = String.Format("{0:P3}", totalAccuracy) };
            if (!fieldAccuracyRows.TryGetValue("OVERALL", out var overallFieldAccuracyRow))
            {
                overallFieldAccuracyRow = new CompareTableRow { Name = "OVERALL", Cells = [overallCell] };
                fieldAccuracyRows.Add("OVERALL", overallFieldAccuracyRow);
            }
            else
            {
                overallFieldAccuracyRow.Cells!.Add(overallCell);
            }
        }

        FieldsAccuracy!.Rows!.Clear();
        FieldsAccuracy.Rows.AddRange(fieldAccuracyRows.Values);
    }


    private void OnFiltersChanged(List<FilterPanelModel> filters)
    {
        InProgressIndicator!.Show("refreshing view from filters");
        Refresh(filters);
        RefreshFailures(filters);
        InProgressIndicator!.Hide();
        StateHasChanged();
    }

    private void HandleButtonClick(CompareTableCell cell)
    {
        UserSession!.Items[nameof(ExperimentRunResult)] = cell.Data![nameof(ExperimentRunResult)];
        UserSession!.Items[nameof(ExperimentMetric)] = cell.Data![nameof(ExperimentMetric)];
        NavigationManager!.NavigateTo($"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{cell.Data!["RunId"]}/detail");
    }
}

public class FieldAccuracy
{
    public string? Name { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }
}

public enum FilterPanelModelComprisons
{
    Equal,
    NotEqual
}

public class FilterPanelModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public FilterPanelModelComprisons Comprison { get; set; }

    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class ExperimentRunResultModel(ExperimentRunResult RunResult, ExperimentMetric Metric, GroundTruthImage GroundTruth, Evaluation eval)
{
    public readonly double InferenceTime = Metric.Value!.Value;

    public ExperimentRunResult RunResult { get; } = RunResult;
    public ExperimentMetric Metric { get; } = Metric;
    public GroundTruthImage GroundTruth { get; } = GroundTruth;
    public List<AssertionModel> Assertions { get; } = eval.GetAssertions(GroundTruth, RunResult.Text!);
}

public class ExpRunModel(FileSystemApi Client, Guid ProjectId, Guid ExperimentId, ExperimentRun ExperimentRun, ILogger Logger)
{
    public readonly List<ExperimentRunResultModel> Results = [];

    public string Name => ExperimentRun.GetIdOrDescription();

    public Guid Id => ExperimentRun.Id;

    public async Task InitAsync(Evaluation eval)
    {
        var results = await Client!.GetExperimentRunResultsAsync(ProjectId, ExperimentId, ExperimentRun.Id);
        if (results is null)
        {
            Logger!.LogError("unable to get experiment run");
            return;
        }

        var metrics = await Client!.GetExperimentRunMetricsAsync(ProjectId, ExperimentId, ExperimentRun.Id);
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

            if (metric.Name != "inference_time")
            {
                Logger!.LogError("metric name is not implemented: {metric_name}", metric.Name);
                return;
            }

            if (!metric.Meta!.TryGetValue("image_file_path", out var imagePathObj))
            {
                Logger!.LogError("unable to get image file path");
                return;
            }

            var imagePath = imagePathObj.Replace(".jpg", ".json");
            var groundTruthResponse = await Client.GetJsonAsync<GroundTruthImage>(imagePath);
            if (!groundTruthResponse.Success)
            {
                Logger!.LogError("unable to get ground truth json for {imagePath}", imagePath);
                return;
            }

            Results.Add(new ExperimentRunResultModel(res, metric, groundTruthResponse.Result!, eval));
        }
    }
}