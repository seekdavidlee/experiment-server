namespace ExperimentServer.Models;

public class ExperimentModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public DateTime? Created { get; set; }
}

public record ExperimentRunModel
{
    public bool IsSelected { get; set; }
    public ExperimentRun? Value { get; set; }
}

public class ExperimentRun
{
    public Guid Id { get; set; }
    public Guid ExperimentId { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets where the experiment runner should pull ground truths from.
    /// </summary>
    public string? DataSetFileSystemApiPath { get; set; }

    /// <summary>
    /// Gets or sets where the experiment runner should persist the final outputs using the FileSystemApi.
    /// </summary>
    /// <remarks>This can include metrics and logs.</remarks>
    public string? OutputFileSystemApiPath { get; set; }

    public string? SystemPrompt { get; set; }
    public string? UserPrompt { get; set; }
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public float TopP { get; set; }
    public string? ModelId { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public int Iterations { get; set; }
}

public enum ExperimentLogLevel
{
    Information,
    Warning,
    Error
}

public class ExperimentLog
{
    public string? Message { get; set; }
    public ExperimentLogLevel? Level { get; set; }
    public DateTime? Created { get; set; }
    public bool? LastLog { get; set; }
}

public class ExperimentMetric
{
    public string? Name { get; set; }
    public double? Value { get; set; }
    public Dictionary<string, string>? Meta { get; set; }
}
