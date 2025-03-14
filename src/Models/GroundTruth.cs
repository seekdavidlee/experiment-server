namespace ExperimentServer.Models;


public class GroundTruthImage
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public DataSetModelField[]? Fields { get; set; }
    public GroundTruthTag[]? Tags { get; set; }
}

public class GroundTruth
{
    public string? Id { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public GroundTruthTag[]? Tags { get; set; }
    public string? Type { get; set; }
}

public class GroundTruthTag
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class GroundTruthReference
{
    public string? Id { get; set; }
    public GroundTruthTag[]? Tags { get; set; }
    public string? Type { get; set; }
}
