namespace ExperimentServer.Models;

public record JobRunLog(DateTime RunTime, string JobName, bool Success, long DurationInMilliseconds);

