using System.Text.Json;

namespace ExperimentServer.Models;

public class DataSetModel
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }

    public DataSetModelField[]? Fields { get; set; }
}

public class DataSetModelField
{
    public string? Name { get; set; }

    public string? Expression { get; set; }

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