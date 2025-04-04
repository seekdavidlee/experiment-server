using ExperimentServer.Models;
using System.Text.Json;

namespace ExperimentServer.Services;

public class Evaluation
{
    private const string JSON_PATTERN = "```json";
    public List<AssertionModel> GetAssertions(GroundTruthImage groundTruthImage, string text)
    {
        List<AssertionModel> assertions = [];
        JsonDocument? doc = null;
        try
        {
            string cleanup = text;
            // special cleanup
            int index = cleanup.IndexOf(JSON_PATTERN);
            if (index >= 0)
            {
                cleanup = cleanup.Substring(index + JSON_PATTERN.Length);
                int lastIndex = cleanup.LastIndexOf('}');
                if (lastIndex > 0)
                {
                    cleanup = cleanup[..(lastIndex + 1)];
                }
            }
            doc = JsonSerializer.Deserialize<JsonDocument>(cleanup!);
        }
        catch (JsonException)
        {

        }
        catch (ArgumentOutOfRangeException)
        {

        }

        foreach (var field in groundTruthImage.Fields!)
        {
            if (field.IsSubjective)
            {
                // skip because we cannnot really compare objectively
                continue;
            }

            if (doc is null)
            {
                assertions.Add(new AssertionModel(field.Name!, field.Value!, "", false, "Failed to parse the result JSON"));
                continue;
            }

            if (doc!.RootElement.TryGetProperty(field.Name!, out var property))
            {
                try
                {
                    bool compare;
                    string actual = property.GetString()!;

                    // cleanup money field
                    if (field.Expression == "money" && actual.StartsWith("$"))
                    {
                        actual = actual[1..];
                    }

                    if (field.Expression == "money" && field.Value!.EndsWith(".00") && !actual.Contains('.'))
                    {
                        // assume .00 if missing, since it is the same anyways
                        compare = string.Compare(field.Value!, $"{actual.Replace(",", "")}.00", true) == 0;
                    }
                    else if (field.Expression == "money" && !field.Value!.Contains('.') && actual.EndsWith(".00"))
                    {
                        compare = string.Compare($"{field.Value!}.00", actual.Replace(",", ""), true) == 0;
                    }
                    else
                    {
                        if (field.Expression == "money")
                        {
                            compare = string.Compare(field.Value!, actual.Replace(",", ""), true) == 0;
                        }
                        else
                        {
                            compare = string.Compare(field.Value!, actual, true) == 0;
                        }
                    }
                    assertions.Add(new AssertionModel(field.Name!, field.Value!, actual, compare, default));
                }
                catch (InvalidOperationException e)
                {
                    assertions.Add(new AssertionModel(field.Name!, field.Value!, "", false, e.Message));
                }
            }
            else
            {
                assertions.Add(new AssertionModel(field.Name!, field.Value!, "", false, "Field not found in the result JSON"));
            }
        }

        return assertions;
    }
}
