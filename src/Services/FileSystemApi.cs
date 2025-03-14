using ExperimentServer.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ExperimentServer.Services;

public class FileSystemApi
{
    private readonly HttpClient httpClient;
    private readonly string PATH_PREFIX;
    private readonly string FILE_PATH_PREFIX;
    private readonly string datasetsFilePath;
    private readonly string projectsFilePath;
    private readonly Config config;
    private readonly ILogger<FileSystemApi> logger;
    public FileSystemApi(Config config, IHttpClientFactory httpClientFactory, ILogger<FileSystemApi> logger)
    {
        httpClient = httpClientFactory.CreateClient(nameof(Config.FileSystemApi));
        PATH_PREFIX = $"{config.FileSystemApi}/storage/files/object?path={config.Environment}/experiment-server";
        FILE_PATH_PREFIX = $"{config.FileSystemApi}/storage/files?path={config.Environment}/experiment-server";
        datasetsFilePath = $"{PATH_PREFIX}/meta/datasets.json";
        projectsFilePath = $"{PATH_PREFIX}/meta/projects.json";
        this.config = config;
        this.logger = logger;
    }

    public string GetDataSetPath(Guid dataSetId)
    {
        return $"{FILE_PATH_PREFIX}/datasets/{dataSetId}/";
    }

    public async Task<List<GroundTruthReference>> GetGroundTruthReferencesAsync()
    {
        var path = $"{PATH_PREFIX}/ground-truths/references/{Constants.ImageInferenceTypes.Receipts}.json";
        var response = await httpClient!.GetAsync(path);
        if (response.IsSuccessStatusCode)
        {
            return await response.DeserializeResponse<List<GroundTruthReference>>();
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return [];
    }

    public async Task<List<DataSetModel>> GetDataSetsAsync()
    {
        var response = await httpClient!.GetAsync(datasetsFilePath);
        if (response.IsSuccessStatusCode)
        {
            return await response.DeserializeResponse<List<DataSetModel>>();
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return [];
    }

    public async Task<ApiResponse> SaveDataSetsAsync(List<DataSetModel> dataSets)
    {
        var json = JsonSerializer.Serialize(dataSets);
        var response = await httpClient.PutAsync(datasetsFilePath, new StringContent(json));

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
            return new ApiResponse(false, content);
        };

        return new ApiResponse(true, default);
    }

    public async Task<List<ProjectModel>> GetProjectsAsync()
    {
        var response = await httpClient!.GetAsync(projectsFilePath);
        if (response.IsSuccessStatusCode)
        {
            return await response.DeserializeResponse<List<ProjectModel>>();
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return [];
    }

    public async Task<ApiResponse> SaveProjectsAsync(List<ProjectModel> projects)
    {
        var json = JsonSerializer.Serialize(projects);
        var response = await httpClient.PutAsync(projectsFilePath, new StringContent(json));

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
            return new ApiResponse(false, content);
        };

        return new ApiResponse(true, default);
    }

    public async Task SaveGroundTruthAsync(GroundTruth groundTruth)
    {
        var json = JsonSerializer.Serialize(groundTruth);
        var response = await httpClient.PutAsync($"{PATH_PREFIX}/ground-truths/items/{Constants.ImageInferenceTypes.Receipts}/{groundTruth.Id}.json", new StringContent(json));

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        }
    }

    public async Task<Prompts> GetPromptsAsync()
    {
        var response = await httpClient!.GetAsync($"{PATH_PREFIX}/prompts/{Constants.ImageInferenceTypes.Receipts}/{Constants.PromptsFilename}");
        if (response.IsSuccessStatusCode)
        {
            var p = await response.Content.ReadFromJsonAsync<Prompts>();
            if (p is null)
            {
                logger.LogError("error: failed to deserialize prompts");
                return new Prompts("", "");
            }

            return p;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new Prompts("", "");
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return new Prompts("", "");
    }

    public async Task<ResponseResult> SavePromptsAsync(Prompts prompts)
    {
        var response = await httpClient.PutAsync($"{PATH_PREFIX}/prompts/{Constants.ImageInferenceTypes.Receipts}/{Constants.PromptsFilename}",
              new StringContent(JsonSerializer.Serialize(prompts)));

        if (response.IsSuccessStatusCode)
        {
            return new ResponseResult(true, null);
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return new ResponseResult(false, content);
    }

    public async Task<List<ExperimentModel>> GetExperimentsAsync(Guid projectId)
    {
        try
        {
            var results = await httpClient!.GetFromJsonAsync<List<ExperimentModel>>(GetExperimentFilePath(projectId));
            return results ?? [];
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }
            logger.LogError(ex, "error: failed to get experiments");
        }
        return [];
    }

    private string GetExperimentFilePath(Guid projectId)
    {
        return $"{PATH_PREFIX}/projects/{projectId}/experiments.json";
    }

    public async Task<ApiResponse> SaveExperimentsAsync(Guid projectId, List<ExperimentModel> experiments)
    {
        var json = JsonSerializer.Serialize(experiments);
        var response = await httpClient.PutAsync(GetExperimentFilePath(projectId), new StringContent(json));

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);

            if (string.IsNullOrEmpty(content)) content = $"Status code: {response.StatusCode}";

            return new ApiResponse(false, content);
        };

        return new ApiResponse(true, default);
    }

    public string GetExperimentRunsPath(Guid projectId, Guid experimentId, bool full)
    {
        string suffix = $"/projects/{projectId}/experiments/{experimentId}/runs";
        return full ? $"{FILE_PATH_PREFIX}{suffix}" :
            $"{config.Environment}/experiment-server{suffix}";
    }

    public async Task<List<ExperimentLog>> GetExperimentRunLogsAsync(Guid projectId, Guid experimentId, Guid runId)
    {
        List<ExperimentLog> logs = [];
        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>($"{GetExperimentRunsPath(projectId, experimentId, true)}/{runId}/logs");
            if (results is null || results.Length == 0) return [];

            // todo: implement paging
            foreach (var path in results)
            {
                var log = await httpClient!.GetFromJsonAsync<ExperimentLog>($"{config.FileSystemApi}/storage/files/object?path={path}");
                if (log is not null)
                {
                    logs.Add(log);
                }
            }

            return [.. logs.OrderBy(x => x.Created)];
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }
            logger.LogError(ex, "error: failed to get experiment run logs");
        }


        return logs;
    }

    public async Task<List<ExperimentRun>> GetExperimentRunsAsync(Guid projectId, Guid experimentId)
    {
        List<ExperimentRun> runs = [];
        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>(GetExperimentRunsPath(projectId, experimentId, true));
            if (results is null || results.Length == 0) return [];


            // todo: implement paging
            foreach (var path in results.Where(x => x.EndsWith("/run.json")))
            {
                var run = await httpClient!.GetFromJsonAsync<ExperimentRun>($"{config.FileSystemApi}/storage/files/object?path={path}");
                if (run is not null)
                {
                    runs.Add(run);
                }
            }

            return runs;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }
            logger.LogError(ex, "error: failed to get experiments");
        }
        return runs;
    }
}
