namespace ExperimentServer.Models;

public class HomePageModel : BasePageModel
{
    public bool? ContentMissing;

    public DailyJobSummary? Summary { get; set; }
}
