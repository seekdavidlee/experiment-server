using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class PerformancePanel
{
    [Parameter]
    public List<ExpRunModel>? RunsResults { get; set; }

    private List<CompareTableModel>? CompareTables { get; set; } = [];

    private string[]? FieldKeys { get; set; }
    private string? SelectedFieldKey { get; set; }
    private bool isComparisonReady;

    protected override void OnInitialized()
    {
        if (RunsResults is not null)
        {
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
            }

            FieldKeys = [.. hash];


        }
    }

    private void Apply()
    {
        if (SelectedFieldKey is not null)
        {
            isComparisonReady = false;

            Dictionary<string, Dictionary<string, int>> multiClassifer = [];
            HashSet<string> uniqueValues = new(FieldKeys!.Select(f => f.ToLower()));

            for (int i = 0; i < RunsResults!.Count; i++)
            {
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
                    Cells = kvp.Value.Select(kvp => new CompareTableCell { Value = kvp.Value.ToString() }).ToList()
                }).ToList();
            }

            isComparisonReady = true;
        }
    }
}
