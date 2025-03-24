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
                        new CompareTableColumn { Name = "Recall" },
                        new CompareTableColumn { Name = "Precision" }
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


            HashSet<string> uniqueValues = new(FieldKeys!.Select(f => f.ToLower()));


            for (int i = 0; i < RunsResults!.Count; i++)
            {
                Dictionary<string, Dictionary<string, int>> multiClassifer = [];
                var run = RunsResults[i];
                foreach (var res in run.Results)
                {
                    foreach (var a in res.Assertions.Where(x => x.Field == SelectedFieldKey))
                    {
                        var expectedKey = a.Expected.ToLower();
                        var actualKey = string.IsNullOrEmpty(a.Actual) ? "_null_or_empty_" : (uniqueValues!.Contains(a.Actual.ToLower()) ? "_invalid_option_" : a.Actual.ToLower());

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

                CompareTables![i].ColumnNames = multiClassifer.Keys.Select(k => new CompareTableColumn { Name = k }).ToArray();
                CompareTables[i].Rows = multiClassifer.Select(kvp => new CompareTableRow
                {
                    Name = kvp.Key,
                    Cells = multiClassifer.Keys.Select(x =>
                    {
                        string val = multiClassifer[kvp.Key].TryGetValue(x, out var value) ? value.ToString() : "0";
                        return new CompareTableCell { Value = val };
                    }).ToList()
                }).ToList();

                // Precision = True Positives (TP) / (True Positives + False Positives)
                // Recall = True Positives(TP) / (True Positives + False Negatives)
                foreach (var category in multiClassifer.Keys)
                {
                    var row = multiClassifer[category];
                    if (!row.ContainsKey(category))
                    {
                        RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                        {
                            Name = category,
                            Cells =
                            [
                                new CompareTableCell{ Value ="N/A" },
                                new CompareTableCell{ Value ="N/A" },
                            ]
                        });
                        continue;
                    }

                    var truthPositive = row[category];
                    var falseNegativePlusTruePositive = row.Values.Sum();
                    // recall
                    var recallCell = new CompareTableCell { Value = "N/A" };
                    if (falseNegativePlusTruePositive > 0)
                    {
                        recallCell.Value = String.Format("{0:F3}", (truthPositive / (double)falseNegativePlusTruePositive));
                    }

                    var precisionCell = new CompareTableCell { Value = "N/A" };
                    var falsePositivePlusTruePositive = multiClassifer.Values.SelectMany(x => x).Where(x => x.Key == category).Sum(x => x.Value);
                    if (falsePositivePlusTruePositive > 0)
                    {
                        precisionCell.Value = String.Format("{0:F3}", (truthPositive / (double)falsePositivePlusTruePositive));
                    }

                    RecallPrecisionTables![i].Rows!.Add(new CompareTableRow
                    {
                        Name = category,
                        Cells = [recallCell, precisionCell]
                    });
                }
            }

            InProgressIndicator!.Hide();
            isComparisonReady = true;
        }
    }
}
