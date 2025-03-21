using ExperimentServer.Models;
using System.Net.Http.Json;

namespace ExperimentServer.Services;

public class InferenceApi(Config config, IHttpClientFactory httpClientFactory, ILogger<InferenceApi> logger)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(nameof(Config.InferenceApi));


    public async Task<ApiResponse<string[]>> GetModelsAsync()
    {
        try
        {
            var results = await httpClient.GetFromJsonAsync<string[]>($"{config.InferenceApi}/inference/models");
            if (results is not null)
                return new ApiResponse<string[]>(true, default, results);
        }
        catch (Exception ex)
        {
            return new ApiResponse<string[]>(false, ex.ToString(), []);
        }
        return new ApiResponse<string[]>(false, "no results are returned", []);
    }

    public async Task<ApiResponse> RunExperimentAsync(ExperimentRun experimentRun)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{config.InferenceApi}/experiments/images", experimentRun);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
                return new ApiResponse(false, content);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error: failed to run experiment");
            return new ApiResponse(false, ex.ToString());
        }

        return new ApiResponse(true, default);
    }
}
