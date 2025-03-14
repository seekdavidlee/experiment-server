namespace ExperimentServer.Models;

public record ImageInferenceResult(double DurationInSeconds, double TimeToFirstTokenInSeconds, int TokenCount, string Text, string SystemPrompt, string UserPrompt, string[]? Base64PdfJpgImages);

