namespace ExperimentServer.Models;

public class NewGroundTruthModel(string type)
{
    public bool IsProcessing { get; set; }
    public string? ImageFilename { get; set; }
    public string? ImageFileContentType { get; set; }

    public string[]? Base64Images { get; set; }

    public readonly List<ImageInferenceResult> InferenceResults = [];

    public int SelectedTabIndex;

    public string? Type
    {
        get
        {
            return type;
        }
    }

    public string? UserPrompt { get; set; }
    public string? SystemPrompt { get; set; }

    public string? Answer { get; set; }
}
