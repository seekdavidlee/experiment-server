namespace ExperimentServer.Models;

public record ApiResponse(bool Success, string? ErrorMessage);

public record ApiResponse<T>(bool Success, string? ErrorMessage, T Result);
