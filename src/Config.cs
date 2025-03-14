namespace ExperimentServer;

public class Config
{
    public string? Environment { get; set; }
    public string? ApiBaseUrl { get; set; }

    public string? FileSystemApi { get; set; }

    public string? FileSystemImagePath { get; set; }

    public string? ImageConversionApi { get; set; }

    public string? InferenceApiBaseUrl { get; set; }

    public string? InferenceApi { get; set; }
}
