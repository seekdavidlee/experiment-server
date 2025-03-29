using System.Text.Json;

namespace ExperimentServer.Models;

public class DataSetModel
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }

    public DataSetPurposes Purpose { get; set; }

    public DataSetModelTypes Type { get; set; }

    public DataSetModelField[]? Fields { get; set; }
}

public enum DataSetPurposes
{
    Unspecified,
    Training,
    Validation,
    Test
}

public enum DataSetModelTypes
{
    Unspecified,
    Images
}

public class DataSetModelField
{
    private const string ListPrefix = "list:";

    public string? Name { get; set; }

    public string? Expression { get; set; }

    public bool IsList
    {
        get
        {
            return Expression is not null && Expression!.StartsWith(ListPrefix);
        }
    }

    public string[] GetListFromExpression()
    {
        return Expression![ListPrefix.Length..].Split(',').Select(x => x.Trim()).ToArray();
    }

    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets if field is subjective and cannot be matched exactly.
    /// </summary>
    /// <remarks>requires the use of LLM as judge as assert for how close the intent matches.</remarks>
    public bool IsSubjective { get; set; }

    public DataSetModelField Clone()
    {
        return JsonSerializer.Deserialize<DataSetModelField>(JsonSerializer.Serialize(this))!;
    }
}