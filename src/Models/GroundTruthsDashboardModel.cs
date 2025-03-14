using ExperimentServer.Models;

namespace Eklee.Reports.Client.Models;

public class GroundTruthsDashboardModel(string type) : BasePageModel
{
    public bool AddNewGroundTruth { get; set; }

    public List<GroundTruthReference> GroundTruthReferences { get; set; } = [];

    public readonly NewGroundTruthModel NewGroundTruthModel = new(type);

    public bool IsSavingGroundTruth = false;
}
