using ExperimentServer.Models;
using Microsoft.AspNetCore.Components;

namespace ExperimentServer.Shared;

public partial class DataSetStats
{
    [Parameter]
    public List<GroundTruthImage>? GroundTruthImages { get; set; }

    [Parameter]
    public DataSetModel? DataSetModel { get; set; }

    private readonly List<ListField> ListFields = [];

    private const string ListPrefix = "list:";

    public bool IsReady { get; set; }

    protected override void OnInitialized()
    {
        if (GroundTruthImages is not null && DataSetModel is not null && DataSetModel.Fields is not null)
        {
            foreach (var listField in DataSetModel.Fields.Where(x => x.Expression is not null && x.Expression.StartsWith(ListPrefix)))
            {
                var splitItems = listField.Expression![ListPrefix.Length..].Split(',').Select(x => x.Trim());

                var list = new ListField(listField.Name!, splitItems.Select(x => new ListFieldValueCount(x)).ToArray());
                foreach (var gt in GroundTruthImages)
                {
                    var f = gt.Fields!.SingleOrDefault(x => x.Name == listField.Name);
                    if (f is not null)
                    {
                        var item = list.ValueCounts.SingleOrDefault(x => string.Compare(x.Name, f.Value, true) == 0);
                        if (item is not null)
                        {
                            item.Count++;
                        }
                    }
                }

                ListFields.Add(list);
            }
        }

        IsReady = true;
    }
}

public record ListField(string Name, ListFieldValueCount[] ValueCounts);

public class ListFieldValueCount(string Name)
{
    public int Count { get; set; }
    public string Name { get; } = Name;
}
