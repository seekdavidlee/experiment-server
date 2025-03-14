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
}