using ExperimentServer.Shared;

namespace ExperimentServer.Models;

public class DataSetManagerModel
{
    public List<GroundTruthImage> Items { get; } = [];

    private List<GroundTruthImage> internalItems = [];

    public void Reset()
    {
        Items.Clear();
        internalItems.Clear();
    }

    public void Update(List<GroundTruthImage> Items)
    {
        internalItems.AddRange(Items);
        RefreshDisplayedItems();
    }

    public bool IsSaving = false;

    private readonly List<FilterPanelModel> internalFilters = [];

    public void ApplyFilters(List<FilterPanelModel> filters)
    {
        internalFilters.Clear();
        internalFilters.AddRange(filters);
        RefreshDisplayedItems();
    }

    private void RefreshDisplayedItems()
    {
        Items.Clear();
        Items.AddRange(internalItems.Where(x =>
        {
            return internalFilters.All(y =>
            {
                if (x.Fields is null) return false;

                var f = x.Fields.SingleOrDefault(b => b.Name == y.Name);
                if (f is null) return false;

                var same = string.Compare(f.Value, y.Value, true) == 0;
                return y.Comprison == FilterPanelModelComprisons.Equal ? same : !same;
            });
        }));
    }
}
