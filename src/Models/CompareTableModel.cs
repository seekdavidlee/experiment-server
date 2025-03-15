namespace ExperimentServer.Models;

public class CompareTableModel
{
    public CompareTableColumn[]? ColumnNames { get; set; }
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

public class CompareTableColumn
{
    public string? Name { get; set; }

    public string? HyperLink { get; set; }
}
