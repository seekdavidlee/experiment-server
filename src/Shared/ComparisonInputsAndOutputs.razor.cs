using ExperimentServer.Models;
using ExperimentServer.Services;
using FastMember;
using MathNet.Numerics.Statistics;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class ComparisonInputsAndOutputs
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
    private ILogger<ComparisonInputsAndOutputs>? Logger { get; set; }

    private CompareTableModel? Inputs;
    private CompareTableModel? Outputs;

    protected override async Task OnInitializedAsync()
    {
        InitInputs(Runs!);

        await InitOutputsAsync(Runs!);
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

                row.Cells!.Add(new CompareTableCell
                {
                    Value = accessor[experimentRun, p].ToString(),
                    FormatText = p == nameof(ExperimentRun.SystemPrompt) || p == nameof(ExperimentRun.UserPrompt)
                });
            }
        }

        Inputs = new CompareTableModel { Title = "Inputs", ColumnNames = [.. colNames.Select(x => new CompareTableColumn { Name = x })], Rows = [.. rows.Values] };
    }

    private async Task InitOutputsAsync(List<ExperimentRun> runs)
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
        outputRows.Add("AvgInferenceTime", new CompareTableRow { Name = "Avg Inference (secs)", Cells = [] });
        outputRows.Add("MinInferenceTime", new CompareTableRow { Name = "Min Inference (secs)", Cells = [] });
        outputRows.Add("MaxInferenceTime", new CompareTableRow { Name = "Max Inference (secs)", Cells = [] });
        outputRows.Add("StdInferenceTime", new CompareTableRow { Name = "Std Deviation Inference", Cells = [] });
        outputRows.Add("TotalRuns", new CompareTableRow { Name = "Total Runs", Cells = [] });
        outputRows.Add("TotalRunTime", new CompareTableRow { Name = "Total Run Time", Cells = [] });

        foreach (var experimentRun in runs)
        {
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

            outputRows["TotalRuns"].Cells!.Add(new CompareTableCell { Value = results.Count.ToString() });

            var ts = (experimentRun.End!.Value - experimentRun.Start!.Value);
            outputRows["TotalRunTime"].Cells!.Add(new CompareTableCell { Value = string.Format("{0:%m} minutes and {0:%s} seconds", ts) });

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
            }

            var avgInferenceTime = TimeSpan.FromMilliseconds(inferenceTimes.Average());
            outputRows["AvgInferenceTime"].Cells!.Add(new CompareTableCell { Value = String.Format("{0:F2}", avgInferenceTime.TotalSeconds) });

            var minInferenceTime = TimeSpan.FromMilliseconds(inferenceTimes.Min());
            outputRows["MinInferenceTime"].Cells!.Add(new CompareTableCell { Value = String.Format("{0:F2}", minInferenceTime.TotalSeconds) });

            var maxInferenceTime = TimeSpan.FromMilliseconds(inferenceTimes.Max());
            outputRows["MaxInferenceTime"].Cells!.Add(new CompareTableCell { Value = String.Format("{0:F2}", maxInferenceTime.TotalSeconds) });

            double stdInference = Statistics.StandardDeviation(inferenceTimes);
            var stdInferenceTime = TimeSpan.FromMilliseconds(stdInference);
            double stdInferenceTimePercentage = stdInference / Statistics.Mean(inferenceTimes);

            outputRows["StdInferenceTime"].Cells!.Add(new CompareTableCell { Value = $"{stdInferenceTime.TotalSeconds:F2} secs ({stdInferenceTimePercentage:P2})" });


        }

        Outputs.Rows!.AddRange(outputRows.Values);
    }
}
