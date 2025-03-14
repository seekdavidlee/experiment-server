using ExperimentServer.Models;

namespace Eklee.Reports.Client.Models;

public class DataSetDashboardModel
{
    public readonly List<DataSetModel> Items = [];
    
    public bool IsSaving = false;
}
