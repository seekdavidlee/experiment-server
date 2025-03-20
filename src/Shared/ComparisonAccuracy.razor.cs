
using ExperimentServer.Models;
using ExperimentServer.Pages.Experiments;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

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
    private ILogger<ComparisonAccuracy>? Logger { get; set; }

    private CompareTableModel? FieldsAccuracy;
    private readonly List<CompareTableModel> PerRunFailureComparisions = [];

    protected override async Task OnInitializedAsync()
    {
        await InitAsync(Runs!);
    }

    private async Task InitAsync(List<ExperimentRun> runs)
    {
        List<(string, Guid)> fieldAccuracyColNames = [];
        Evaluation eval = new();
        Dictionary<string, CompareTableRow> fieldAccuracyRows = [];
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
                    new CompareTableColumn { Name = "Message" },
                    new CompareTableColumn { Name = "Tags" }
                ]
            };

            fieldAccuracyColNames.Add((experimentRun.GetIdOrDescription(), experimentRun.Id));
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

            List<double> inferenceTimes = [];
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

                if (metric.Name == "inference_time")
                {
                    inferenceTimes.Add(metric.Value!.Value);
                }

                var imagePath = imagePathObj.Replace(".jpg", ".json");
                var groundTruthResponse = await Client.GetJsonAsync<GroundTruthImage>(imagePath);
                if (!groundTruthResponse.Success)
                {
                    Logger!.LogError("unable to get ground truth json for {imagePath}", imagePath);
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
                            newPerFieldFailureComparisionRow.Cells!.Add(new CompareTableCell { Value = groundTruthResponse.Result.Tags.GetString() });
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

            if (perRunFailureComparision.Rows!.Count != 0)
            {
                // cleanup row name
                foreach (var row in perRunFailureComparision.Rows!)
                {
                    var i = row.Name!.LastIndexOf('.');
                    row.Name = row.Name[..i];
                }
                PerRunFailureComparisions.Add(perRunFailureComparision);
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
            Rows = [.. fieldAccuracyRows.Values]
        };
    }
}

public class FieldAccuracy
{
    public string? Name { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }
}