using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class FiltersPanel
{
    private readonly List<GroundTruthTagModel> Filters = [];

    private void AddFilter()
    {
        Filters.Add(new GroundTruthTagModel(new GroundTruthTag()));
    }

    [Parameter]
    public Action<List<GroundTruthTagModel>>? OnUpdates { get; set; }

    [Parameter]
    public string[]? Keys { get; set; }

    private void OnDeleteFilter(GroundTruthTagModel filter)
    {
        var list = Filters.Where(x => x.Id != filter.Id).ToList();
        Filters.Clear();
        Filters.AddRange(list);

        OnUpdates!(Filters);
    }

    private void OnFilterChanged()
    {
        OnUpdates!(Filters);
    }
}
