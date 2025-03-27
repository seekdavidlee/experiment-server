using ExperimentServer.Models;
using System.Text.Json;

namespace ExperimentServer;

public static class Extensions
{
    public static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<T> DeserializeResponse<T>(this HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync()!, options)!;
    }

    public static void ClearRows(this List<CompareTableModel> tables)
    {
        foreach (var table in tables)
        {
            if (table.Rows is not null)
            {
                table.Rows.Clear();
            }
        }
    }
}
