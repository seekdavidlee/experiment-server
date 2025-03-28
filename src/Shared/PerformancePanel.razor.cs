using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class PerformancePanel
{
    [Parameter]
    public List<ExpRunModel>? RunsResults { get; set; }

    private List<CompareTableModel>? CompareTables { get; set; } = [];
    private List<CompareTableModel>? RecallPrecisionTables { get; set; } = [];

    [Inject]
    private InProgressIndicatorService? InProgressIndicator { get; set; }

    private string[]? FieldKeys { get; set; }
    private string? SelectedFieldKey { get; set; }
    private bool isComparisonReady;

    protected override void OnInitialized()
    {
        if (RunsResults is not null)
        {
            InProgressIndicator!.Show("loading performance data");
            var hash = new HashSet<string>();
            foreach (var run in RunsResults)
            {
                foreach (var res in run.Results)
                {
                    foreach (var a in res.Assertions)
                    {
                        hash.Add(a.Field);
                    }
                }

                var compareTable = new CompareTableModel()
                {
                    Title = $"Multiclass classification: {run.Name}",
                    ColumnKey = "Expected ==> \nAcutal ==v"
                };

                CompareTables!.Add(compareTable);

                var recallPrecisionTable = new CompareTableModel()
                {
                    Title = $"Recall and Precision: {run.Name}",
                    ColumnNames =
                    [
                        new CompareTableColumn { Name = "Size" },
                        new CompareTableColumn { Name = "Recall" },
                        new CompareTableColumn { Name = "Precision" },
                        new CompareTableColumn { Name = "F1" }
                    ],
                    Rows = []
                };
                RecallPrecisionTables!.Add(recallPrecisionTable);
            }

            FieldKeys = [.. hash];
            InProgressIndicator!.Hide();
        }
    }

    private void Apply()
    {
        if (SelectedFieldKey is not null)
        {
            InProgressIndicator!.Show("loading performance data");
            isComparisonReady = false;

            CompareTables!.ClearRows();
            RecallPrecisionTables!.ClearRows();

            for (int i = 0; i < RunsResults!.Count; i++)
            {
                // todo: this should come from the user because the user knows all the expected fields.
                HashSet<string> expectedFieldKeys = [];

                HashSet<Guid> seenGroundTruths = [];    // prevent duplicate ground truths from being counted
                Dictionary<string, int> sampleSize = [];
                Dictionary<string, Dictionary<string, int>> multiClassifer = [];
                var run = RunsResults[i];
                foreach (var res in run.Results)
                {
                    if (!seenGroundTruths.Contains(res.GroundTruth.Id))
                    {
                        var sample = res.GroundTruth.Fields!.SingleOrDefault(x => x.Name == SelectedFieldKey);
                        if (sample is not null)
                        {
                            if (sampleSize.TryGetValue(sample.Value!.ToLower(), out int value))
                            {
                                sampleSize[sample.Value.ToLower()] = ++value;
                            }
                            else
                            {
                                sampleSize[sample.Value.ToLower()] = 1;
                            }

                            seenGroundTruths.Add(res.GroundTruth.Id);
                        }
                    }

                    foreach (var a in res.Assertions.Where(x => x.Field == SelectedFieldKey))
                    {
                        var expectedKey = a.Expected.ToLower();
                        var actualKey = string.IsNullOrEmpty(a.Actual) ? "_null_or_empty_" : a.Actual.ToLower();

                        expectedFieldKeys.Add(expectedKey);

                        if (!multiClassifer.TryGetValue(expectedKey, out Dictionary<string, int>? compareDic))
                        {
                            compareDic = [];
                            multiClassifer[expectedKey] = compareDic;
                        }

                        if (!compareDic.TryGetValue(actualKey, out int value))
                        {
                            value = 0;
                            compareDic[actualKey] = value;
                        }

                        compareDic[actualKey] = ++value;
                    }
                }

                CompareTables![i].ColumnNames = [.. expectedFieldKeys.Select(k => new CompareTableColumn { Name = k }).OrderBy(x => x.Name)];

                string[] actualFieldRowKeys = [.. multiClassifer.Values.SelectMany(x => x).Select(x => x.Key).Concat(multiClassifer.Keys).Distinct().Order()];

                CompareTables[i].Rows = actualFieldRowKeys.Select(kvp => new CompareTableRow
                {
                    Name = kvp,
                    Cells = expectedFieldKeys.Order().Select(x =>
                    {
                        string val = multiClassifer[x].TryGetValue(kvp, out var value) ? value.ToString() : "0";
                        return new CompareTableCell { Value = val };
                    }).ToList()
                }).ToList();

                double f1Total = 0;
                // Precision = True Positives (TP) / (True Positives + False Positives)
                // Recall = True Positives(TP) / (True Positives + False Negatives)
                foreach (var category in multiClassifer.Keys)
                {
                    string target = category!;
                    var row = multiClassifer[category];
                    if (!row.ContainsKey(category))
                    {
                        RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                        {
                            Name = category,
                            Cells =
                            [
                                new CompareTableCell { Value = sampleSize[category].ToString() },
                                new CompareTableCell{ Value ="0" },
                                new CompareTableCell{ Value ="0" },
                                new CompareTableCell{ Value ="0" },
                            ]
                        });
                        continue;
                    }

                    var truthPositive = row[category];
                    var falseNegativePlusTruePositive = row.Values.Sum();

                    var recallCell = new CompareTableCell { Value = "0" };
                    double recall = 0;
                    if (falseNegativePlusTruePositive > 0)
                    {
                        recall = (truthPositive / (double)falseNegativePlusTruePositive);
                        recallCell.Value = String.Format("{0:F3}", recall);
                    }

                    var precisionCell = new CompareTableCell { Value = "0" };
                    double precision = 0;
                    var falsePositivePlusTruePositive = multiClassifer.Values.SelectMany(x => x).Where(x => x.Key == category).Sum(x => x.Value);
                    if (falsePositivePlusTruePositive > 0)
                    {
                        precision = (truthPositive / (double)falsePositivePlusTruePositive);
                        precisionCell.Value = String.Format("{0:F3}", precision);
                    }

                    double f1 = recall > 0 || precision > 0 ? 2 * ((precision * recall) / (precision + recall)) : 0;
                    f1Total += f1;
                    var f1Cell = new CompareTableCell { Value = f1 > 0 ? string.Format("{0:F3}", f1) : "0" };

                    RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                    {
                        Name = category,
                        Cells = [new CompareTableCell { Value = sampleSize[category].ToString() }, recallCell, precisionCell, f1Cell]
                    });
                }

                RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                {
                    Name = "TOTAL",
                    Cells = [new CompareTableCell { Value = sampleSize.Values.Sum().ToString() }, new CompareTableCell { Value = "" }, new CompareTableCell { Value = "" }, new CompareTableCell { Value = "" }]
                });

                // overall F1 score
                double macroF1 = multiClassifer.Keys.Count > 0 ? f1Total / multiClassifer.Keys.Count : 0;
                RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                {
                    Name = "F1 Score (macro avg)",
                    Cells = [new CompareTableCell { Value = "" }, new CompareTableCell { Value = "" }, new CompareTableCell { Value = "" }, new CompareTableCell { Value = macroF1 > 0 ? string.Format("{0:F3}", macroF1) : "0" }]
                });
            }

            InProgressIndicator!.Hide();
            isComparisonReady = true;
        }
    }
}
