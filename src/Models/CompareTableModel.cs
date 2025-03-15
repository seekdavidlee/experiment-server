namespace ExperimentServer.Models;

public class CompareTableModel
{
    public string[]? ColumnNames { get; set; }
    public List<CompareTableRow>? Rows { get; set; }
}

public class CompareTableRow
{
    public string? Name { get; set; }
    public List<CompareTableCell>? Cells { get; set; }
}

public class CompareTableCell
{
    public string? Value { get; set; }
}
