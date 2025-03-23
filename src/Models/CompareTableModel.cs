namespace ExperimentServer.Models;

public class CompareTableModel
{
    public string? Title { get; set; }
    public string ColumnKey { get; set; } = "Name";
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

    /// <summary>
    /// Gets or sets when we have more than one value to display in the cell.
    /// </summary>
    public string[]? Values { get; set; }

    /// <summary>
    /// Gets or sets whether to apply newlines as line breaks.
    /// </summary>
    public bool? FormatText { get; set; }
}

public class CompareTableColumn
{
    public string? Name { get; set; }

    public string? HyperLink { get; set; }

    public bool? Hide { get; set; }
}
