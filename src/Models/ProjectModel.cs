namespace ExperimentServer.Models;

public class ProjectModel
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
}
public class ProjectDashboardModel
{
    public readonly List<ProjectModel> Items = [];

    public bool IsSaving = false;
}