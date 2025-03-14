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

