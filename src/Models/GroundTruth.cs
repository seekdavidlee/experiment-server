using System.Text;

namespace ExperimentServer.Models;

public class GroundTruthImage
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public DataSetModelField[]? Fields { get; set; }
    public GroundTruthTag[]? Tags { get; set; }
}

public class GroundTruthTag
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public static class Extensions
{
    public static string GetString(this GroundTruthTag[]? tags)
    {
        if (tags is null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var tag in tags)
        {
            sb.Append($"{tag.Name}: {tag.Value}\n");
        }
        return sb.ToString();
    }
}

