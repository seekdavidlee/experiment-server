using ExperimentServer.Models;
using ExperimentServer.Shared;

namespace Eklee.Reports.Client.Models;

public class DataSetDashboardModel
{
    public readonly List<DataSetModel> Items = [];
    
    public bool IsSaving = false;

    public readonly List<FilterPanelModel> Filters = [];
}
