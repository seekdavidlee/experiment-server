using System.Text.Json;

namespace ExperimentServer;

public static class Extensions
{
    public static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<T> DeserializeResponse<T>(this HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync()!, options)!;
    }
}
