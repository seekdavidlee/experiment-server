﻿using ExperimentServer.Models;
using System.Collections.Concurrent;
using System.Data;
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

    public async Task<ApiResponse<ProjectModel?>> GetProjectAsync(Guid projectId)
    {
        var response = await httpClient!.GetAsync(projectsFilePath);
        if (response.IsSuccessStatusCode)
        {
            var projects = await response.DeserializeResponse<List<ProjectModel>>();
            if (projects is null)
            {
                return new ApiResponse<ProjectModel?>(false, "unable to load projects due to an internal error, please retry", default);
            }

            var project = projects.SingleOrDefault(x => x.Id == projectId);
            return new ApiResponse<ProjectModel?>(project is not null, project is null ? "project is not found" : default, project);
        }
        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return new ApiResponse<ProjectModel?>(false, $"error: {content}, http status: {response.StatusCode}", default);
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


    private string GetPromptPath(Guid projectId, Guid experimentId)
    {
        return $"{PATH_PREFIX}/meta/projects/{projectId}/experiments/{experimentId}/prompt.json";
    }

    public async Task<ApiResponse<Prompts?>> GetPromptsAsync(Guid projectId, Guid experimentId)
    {

        var response = await httpClient!.GetAsync(GetPromptPath(projectId, experimentId));
        if (response.IsSuccessStatusCode)
        {
            var p = await response.Content.ReadFromJsonAsync<Prompts>();
            if (p is null)
            {
                logger.LogError("error: failed to deserialize prompts");
                return new ApiResponse<Prompts?>(false, "failed to deserialize prompts", new Prompts("You are an AI assistant", "Tell me about the picture."));
            }

            return new ApiResponse<Prompts?>(true, default, p);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ApiResponse<Prompts?>(true, default, new Prompts("You are an AI assistant", "Tell me about the picture."));
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return new ApiResponse<Prompts?>(false, $"error: {content}, http status: {response.StatusCode}", default);
    }

    public async Task<ApiResponse> SavePromptsAsync(Guid projectId, Guid experimentId, Prompts prompts)
    {
        var response = await httpClient.PutAsync(GetPromptPath(projectId, experimentId),
              new StringContent(JsonSerializer.Serialize(prompts)));

        if (response.IsSuccessStatusCode)
        {
            return new ApiResponse(true, default);
        }

        var content = await response.Content.ReadAsStringAsync();
        logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
        return new ApiResponse(false, content);
    }

    public async Task<List<ExperimentModel>> GetExperimentsAsync(Guid projectId)
    {
        try
        {
            var results = await httpClient!.GetFromJsonAsync<List<ExperimentModel>>(GetExperimentFilePath(projectId));
            return results!.OrderByDescending(x => x.Created).ToList() ?? [];
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

    private readonly Dictionary<string, GroundTruthImage> groundTruthImageCache = [];
    public async Task<ApiResponse<List<GroundTruthImage>>> GetGroundTruthImagesAsync(Guid datasetId)
    {
        List<GroundTruthImage> images = [];
        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>($"{FILE_PATH_PREFIX}/datasets/{datasetId}/");
            if (results is null || results.Length == 0) return new ApiResponse<List<GroundTruthImage>>(true, default, images);

            results = results.Where(x => x.EndsWith(".json")).ToArray();

            // todo: implement paging
            foreach (var path in results)
            {
                if (groundTruthImageCache.TryGetValue(path, out var cachedImg) && cachedImg is not null)
                {
                    images.Add(cachedImg);
                }
                else
                {
                    var img = await httpClient!.GetFromJsonAsync<GroundTruthImage>($"{config.FileSystemApi}/storage/files/object?path={path}");
                    if (img is not null)
                    {
                        images.Add(img);
                        groundTruthImageCache[path] = img;
                    }
                }
            }

            return new ApiResponse<List<GroundTruthImage>>(true, default, images);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new ApiResponse<List<GroundTruthImage>>(true, default, images);
            }
            logger.LogError(ex, "error: failed to get experiment run logs");

            return new ApiResponse<List<GroundTruthImage>>(false, "failed to get experiment run logs", images);
        }
    }

    public async Task<ApiResponse> SaveGroundTruthImageAsync(Guid datasetId, GroundTruthImage groundTruth)
    {
        var json = JsonSerializer.Serialize(groundTruth);
        var response = await httpClient.PutAsync($"{PATH_PREFIX}/datasets/{datasetId}/{groundTruth.Id}.json", new StringContent(json));
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
            return new ApiResponse(false, $"error: {content}, http status: {response.StatusCode}");
        }
        return new ApiResponse(true, default);
    }

    public async Task<ApiResponse> SaveGroundTruthImageAsync(Guid datasetId, Guid groundTruthId, byte[] imageBytes)
    {
        var imageResponse = await httpClient.PutAsync($"{PATH_PREFIX}/datasets/{datasetId}/{groundTruthId}.jpg", new ByteArrayContent(imageBytes));
        if (!imageResponse.IsSuccessStatusCode)
        {
            var content = await imageResponse.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, imageResponse.StatusCode);
            return new ApiResponse(false, $"error: {content}, http status: {imageResponse.StatusCode}");
        }

        return new ApiResponse(true, default);
    }

    public async Task<ApiResponse> DeleteGroundTruthImageAsync(Guid datasetId, GroundTruthImage groundTruthImage)
    {
        try
        {
            var response = await httpClient!.DeleteAsync($"{FILE_PATH_PREFIX}/datasets/{datasetId}/{groundTruthImage.Id}.jpg");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("error: failed to delete ground truth image: {imagePath}", "");
            }

            response = await httpClient!.DeleteAsync($"{FILE_PATH_PREFIX}/datasets/{datasetId}/{groundTruthImage.Id}.json");
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true, default);
            }

            return new ApiResponse(false, $"error: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "error: failed to delete ground truth");
            return new ApiResponse(false, ex.ToString());
        }
    }

    public async Task<ApiResponse> SaveGroundTruthImageAsync(Guid datasetId, GroundTruthImage groundTruth, byte[] imageBytes)
    {
        string path = $"{config.Environment}/experiment-server/datasets/{datasetId}/{groundTruth.Id}.json";
        if (groundTruthImageCache.Remove(path))
        {
            logger.LogInformation("removed path {path} from ground truth cache", path);
        }

        var json = JsonSerializer.Serialize(groundTruth);
        var response = await httpClient.PutAsync($"{PATH_PREFIX}/datasets/{datasetId}/{groundTruth.Id}.json", new StringContent(json));
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, response.StatusCode);
            return new ApiResponse(false, $"error: {content}, http status: {response.StatusCode}");
        }

        var imageResponse = await httpClient.PutAsync($"{PATH_PREFIX}/datasets/{datasetId}/{groundTruth.Id}.jpg", new ByteArrayContent(imageBytes));
        if (!imageResponse.IsSuccessStatusCode)
        {
            var content = await imageResponse.Content.ReadAsStringAsync();
            logger.LogError("error: {content}, http status: {status}", content, imageResponse.StatusCode);
            return new ApiResponse(false, $"error: {content}, http status: {imageResponse.StatusCode}");
        }

        return new ApiResponse(true, default);
    }

    public async Task<ApiResponse<byte[]>> GetGroundTruthImageAsync(Guid datasetId, GroundTruthImage groundTruth)
    {
        try
        {
            var results = await httpClient!.GetByteArrayAsync($"{PATH_PREFIX}/datasets/{datasetId}/{groundTruth.Id}.jpg");
            if (results is not null)
            {
                return new ApiResponse<byte[]>(true, default, results);
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return new ApiResponse<byte[]>(true, default, []);
            }
            return new ApiResponse<byte[]>(false, e.ToString(), []);
        }

        return new ApiResponse<byte[]>(false, "unable to read response", []);
    }

    public async Task<ApiResponse<byte[]>> GetImageAsync(string path)
    {
        try
        {
            var results = await httpClient!.GetByteArrayAsync($"{config.FileSystemApi}/storage/files/object?path={path}");
            if (results is not null)
            {
                return new ApiResponse<byte[]>(true, default, results);
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return new ApiResponse<byte[]>(true, default, []);
            }
            return new ApiResponse<byte[]>(false, e.ToString(), []);
        }

        return new ApiResponse<byte[]>(false, "unable to read response", []);
    }

    public async Task<ApiResponse<T?>> GetJsonAsync<T>(string path) where T : class, new()
    {
        try
        {
            if (typeof(T) == typeof(GroundTruthImage))
            {
                if (groundTruthImageCache.TryGetValue(path, out var image))
                {
                    return new ApiResponse<T?>(true, default, image as T);
                }
            }
            var results = await httpClient!.GetFromJsonAsync<T>($"{config.FileSystemApi}/storage/files/object?path={path}");
            if (results is not null)
            {
                if (typeof(T) == typeof(GroundTruthImage))
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    groundTruthImageCache.Add(path, results as GroundTruthImage);
#pragma warning restore CS8604 // Possible null reference argument.
                }
                return new ApiResponse<T?>(true, default, results);
            }
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return new ApiResponse<T?>(true, default, new T());
            }
            return new ApiResponse<T?>(false, e.ToString(), default);
        }

        return new ApiResponse<T?>(false, "unable to read response", default);
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

    private readonly Dictionary<string, ExperimentRunResult> runResultsCache = [];
    public async Task<List<ExperimentRunResult>> GetExperimentRunResultsAsync(Guid projectId, Guid experimentId, Guid runId)
    {
        List<ExperimentRunResult> items = [];
        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>($"{GetExperimentRunsPath(projectId, experimentId, true)}/{runId}/results");
            if (results is null || results.Length == 0) return [];

            // todo: implement paging
            foreach (var path in results.OrderBy(GetFileNumber))
            {
                if (runResultsCache.TryGetValue(path, out var result) && result is not null)
                {
                    items.Add(result);
                }
                else
                {
                    var item = await httpClient!.GetFromJsonAsync<ExperimentRunResult>($"{config.FileSystemApi}/storage/files/object?path={path}");
                    if (item is not null)
                    {
                        items.Add(item);
                        runResultsCache[path] = item;
                    }
                }
            }

            return items;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }
            logger.LogError(ex, "error: failed to get experiment run results");
        }


        return items;
    }

    private static long GetFileNumber(string path)
    {
        var index = path.LastIndexOf('/');
        var name = path[(index + 1)..].Replace(".json", "");
        return long.Parse(name);
    }

    private readonly Dictionary<string, ExperimentMetric> metricsCache = [];

    public async Task<List<ExperimentMetric>> GetExperimentRunMetricsAsync(Guid projectId, Guid experimentId, Guid runId)
    {
        List<ExperimentMetric> items = [];
        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>($"{GetExperimentRunsPath(projectId, experimentId, true)}/{runId}/metrics");
            if (results is null || results.Length == 0) return [];

            // todo: implement paging
            foreach (var path in results.OrderBy(GetFileNumber))
            {
                if (metricsCache.TryGetValue(path, out var metric) && metric is not null)
                {
                    items.Add(metric);
                }
                else
                {
                    var item = await httpClient!.GetFromJsonAsync<ExperimentMetric>($"{config.FileSystemApi}/storage/files/object?path={path}");
                    if (item is not null)
                    {
                        items.Add(item);
                        metricsCache[path] = item;
                    }
                }
            }

            return items;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }
            logger.LogError(ex, "error: failed to get experiment run metrics");
        }


        return items;
    }

    public async Task<(List<ExperimentRun>, HashSet<Guid>)> GetExperimentRunsAsync(Guid projectId, Guid experimentId, HashSet<Guid>? existingComplete)
    {
        List<ExperimentRun> runs = [];
        HashSet<Guid> fullList = [];

        try
        {
            var results = await httpClient!.GetFromJsonAsync<string[]>(GetExperimentRunsPath(projectId, experimentId, true));
            if (results is null || results.Length == 0) return ([], []);


            // todo: implement paging
            foreach (var path in results.Where(x => x.EndsWith("/run.json")))
            {
                var i = path.IndexOf("/run.json");
                var partPaths = path[..i].Split('/');
                var pathId = Guid.Parse(partPaths[^1]);
                fullList.Add(pathId);
                if (existingComplete is not null && existingComplete.Contains(pathId))
                {
                    continue;
                }

                var run = await httpClient!.GetFromJsonAsync<ExperimentRun>($"{config.FileSystemApi}/storage/files/object?path={path}");
                if (run is not null)
                {
                    runs.Add(run);
                }
            }

            return (runs, fullList);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ([], []);
            }
            logger.LogError(ex, "error: failed to get experiments");
        }
        return (runs, fullList);
    }

    public async Task<ApiResponse<ExperimentRun?>> GetExperimentRunAsync(Guid projectId, Guid experimentId, Guid id)
    {
        try
        {
            string path = $"{GetExperimentRunsPath(projectId, experimentId, false)}/{id}/run.json";
            var run = await httpClient!.GetFromJsonAsync<ExperimentRun>($"{config.FileSystemApi}/storage/files/object?path={path}");
            if (run is not null)
            {
                return new ApiResponse<ExperimentRun?>(true, default, run);
            }

        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "error: failed to get experiments");
            return new ApiResponse<ExperimentRun?>(false, ex.ToString(), default);
        }

        return new ApiResponse<ExperimentRun?>(false, "unable to read response", default);
    }

    public async Task<ApiResponse> DeleteExperimentRunAsync(Guid projectId, Guid experimentId, Guid id)
    {
        try
        {
            string path = $"{GetExperimentRunsPath(projectId, experimentId, false)}/{id}/";
            var response = await httpClient!.DeleteAsync($"{config.FileSystemApi}/storage/files?path={path}");
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true, default);
            }

            return new ApiResponse(false, $"error: {response.StatusCode}");

        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "error: failed to delete experiment run");
            return new ApiResponse(false, ex.ToString());
        }
    }

    public async Task<ApiResponse> DeleteDatasetAsync(Guid id)
    {
        try
        {
            var response = await httpClient!.DeleteAsync($"{FILE_PATH_PREFIX}/datasets/{id}");
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true, default);
            }

            return new ApiResponse(false, $"error: {response.StatusCode}");

        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "error: failed to delete experiment run");
            return new ApiResponse(false, ex.ToString());
        }
    }

    public async Task<ApiResponse> DeleteExperimentAsync(Guid projectId, ExperimentModel experimentModel)
    {
        // first delete all experiment runs
        try
        {
            string path = $"{GetExperimentRunsPath(projectId, experimentModel.Id, false)}/";
            var response = await httpClient!.DeleteAsync($"{config.FileSystemApi}/storage/files?path={path}");
            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse(false, $"error: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "error: failed to delete experiment run");
            return new ApiResponse(false, ex.ToString());
        }

        // then we can delete the experiment from the project
        var experiments = await GetExperimentsAsync(projectId);
        experiments = experiments.Where(x => x.Id != experimentModel.Id).ToList();
        return await SaveExperimentsAsync(projectId, experiments);
    }
}
