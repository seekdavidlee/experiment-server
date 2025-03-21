namespace ExperimentServer.Models;

public record AssertionModel(string Field, string Expected, string? Actual, bool Success, string? Message);
