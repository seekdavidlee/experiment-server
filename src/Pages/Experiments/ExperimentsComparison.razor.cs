﻿
using ExperimentServer.Models;
using ExperimentServer.Services;
using FastMember;
using Microsoft.AspNetCore.Components;
using Radzen;
using System;

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
    private CompareTableModel? Outputs;
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
            colNames.Add(experimentRun.GetIdOrDescription());
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

        Inputs = new CompareTableModel { Title = "Inputs", ColumnNames = [.. colNames.Select(x => new CompareTableColumn { Name = x })], Rows = [.. rows.Values] };
    }

    private async Task InitAsync(List<ExperimentRun> runs)
    {
        Outputs = new CompareTableModel
        {
            Title = "Outputs",
            ColumnNames = runs.Select(x => new CompareTableColumn
            {
                Name = x.GetIdOrDescription(),
                HyperLink = $"/projects/{ProjectId}/experiments/{ExperimentId}/runs/{x.Id}"
            }).ToArray(),
            Rows = []
        };

        Dictionary<string, CompareTableRow> outputRows = [];
        outputRows.Add(nameof(ExperimentRunResult.PromptTokens), new CompareTableRow { Name = $"Total {nameof(ExperimentRunResult.PromptTokens)}", Cells = [] });
        outputRows.Add(nameof(ExperimentRunResult.CompletionTokens), new CompareTableRow { Name = $"Total {nameof(ExperimentRunResult.CompletionTokens)}", Cells = [] });
        outputRows.Add($"Min{nameof(ExperimentRunResult.PromptTokens)}", new CompareTableRow { Name = $"Min {nameof(ExperimentRunResult.PromptTokens)}", Cells = [] });
        outputRows.Add($"Max{nameof(ExperimentRunResult.PromptTokens)}", new CompareTableRow { Name = $"Max {nameof(ExperimentRunResult.PromptTokens)}", Cells = [] });
        outputRows.Add($"Min{nameof(ExperimentRunResult.CompletionTokens)}", new CompareTableRow { Name = $"Min {nameof(ExperimentRunResult.CompletionTokens)}", Cells = [] });
        outputRows.Add($"Max{nameof(ExperimentRunResult.CompletionTokens)}", new CompareTableRow { Name = $"Max {nameof(ExperimentRunResult.CompletionTokens)}", Cells = [] });
        outputRows.Add("TotalRunTime", new CompareTableRow { Name = "Total Run Time", Cells = [] });


        List<(string, Guid)> fieldAccuracyColNames = [];
        Evaluation eval = new();
        Dictionary<string, CompareTableRow> fieldAccuracyRows = [];
        foreach (var experimentRun in runs)
        {
            CompareTableModel perRunFailureComparision = new()
            {
                Title = $"{experimentRun.GetIdOrDescription()} failures",
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

            outputRows[nameof(ExperimentRunResult.PromptTokens)].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.PromptTokens).Sum().ToString() });
            outputRows[nameof(ExperimentRunResult.CompletionTokens)].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.CompletionTokens).Sum().ToString() });
            outputRows[$"Min{nameof(ExperimentRunResult.PromptTokens)}"].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.PromptTokens).Min().ToString() });
            outputRows[$"Max{nameof(ExperimentRunResult.PromptTokens)}"].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.PromptTokens).Max().ToString() });
            outputRows[$"Min{nameof(ExperimentRunResult.CompletionTokens)}"].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.CompletionTokens).Min().ToString() });
            outputRows[$"Max{nameof(ExperimentRunResult.CompletionTokens)}"].Cells!.Add(new CompareTableCell { Value = results.Select(x => x.CompletionTokens).Max().ToString() });

            var ts = (experimentRun.End!.Value - experimentRun.Start!.Value);
            outputRows["TotalRunTime"].Cells!.Add(new CompareTableCell { Value = string.Format("{0:%m} minutes and {0:%s} seconds", ts) });

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

        Outputs.Rows!.AddRange(outputRows.Values);

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