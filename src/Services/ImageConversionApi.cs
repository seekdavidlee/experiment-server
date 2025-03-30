using ExperimentServer.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExperimentServer.Services;

public class ImageConversionApi(Config config, IHttpClientFactory httpClientFactory, ILogger<ImageConversionApi> logger)
{
    private readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(nameof(Config.ImageConversionApi));
    private readonly string url = $"{config.ImageConversionApi}/docs?format=Jpeg&quality=100";
    private const string FILE_NAME = "file.pdf";
    public async Task<ApiResponse<string[]>> ToImagesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var imageData = memoryStream.ToArray();
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "file", FILE_NAME }
        };
        var responseMessage = await httpClient.PostAsync(url, content);
        var responseContent = await responseMessage.Content.ReadAsStringAsync();
        if (responseMessage.IsSuccessStatusCode)
        {
            var pdfImages = JsonSerializer.Deserialize<string[]>(responseContent);
            if (pdfImages is null)
            {
                logger.LogError("failed to convert pdf.");
                return new ApiResponse<string[]>(false, "failed to convert pdf.", []);
            }
            return new ApiResponse<string[]>(true, default, pdfImages);
        }

        if (responseMessage.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            return new ApiResponse<string[]>(false, $"Image is too large, size: {CalculateSizeInMB(imageData)}mb", []);
        }

        logger.LogError("failed to convert pdf. Http Status: {statusCode}, Response: {responseContent}",
            responseMessage.StatusCode, responseContent);
        return new ApiResponse<string[]>(false, $"failed to convert pdf. Http Status: {responseMessage.StatusCode}, Response: {responseContent}", []);
    }

    private static double CalculateSizeInMB(byte[] imageData)
    {
        const double bytesInMB = 1024 * 1024;
        return imageData.Length / bytesInMB;
    }

    public async Task<ApiResponse<ImageInfo?>> GetImageInfo(byte[] imageData)
    {
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "file", FILE_NAME }
        };

        try
        {
            var responseMessage = await httpClient.PostAsync($"{config.ImageConversionApi}/images/info", content);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            if (responseMessage.IsSuccessStatusCode)
            {
                return new ApiResponse<ImageInfo?>(true, default, JsonSerializer.Deserialize<ImageInfo>(responseContent, serializerOptions));
            }

            if (responseMessage.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                return new ApiResponse<ImageInfo?>(false, $"Image is too large, size: {CalculateSizeInMB(imageData)}mb", default);
            }

            logger.LogError("failed to get image info. Http Status: {statusCode}, Response: {responseContent}",
                responseMessage.StatusCode, responseContent);

            return new ApiResponse<ImageInfo?>(false, $"failed to get image info. Http Status: {responseMessage.StatusCode}, Response: {responseContent}", default);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                return new ApiResponse<ImageInfo?>(false, $"Image is too large, size: {CalculateSizeInMB(imageData)}mb", default);
            }

            throw;
        }
    }

    public async Task<ApiResponse<byte[]?>> ResizeImageInfo(byte[] imageData, int percent)
    {
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "file", FILE_NAME }
        };

        var responseMessage = await httpClient.PostAsync($"{config.ImageConversionApi}/images/resize?percent={percent}", content);

        if (responseMessage.IsSuccessStatusCode)
        {
            return new ApiResponse<byte[]?>(true, default, await responseMessage.Content.ReadAsByteArrayAsync());
        }

        if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
        {
            var err = await responseMessage.Content.ReadFromJsonAsync<ErrorModel>();
            return new ApiResponse<byte[]?>(false, err!.ErrorMessage, default);
        }
        var responseContent = await responseMessage.Content.ReadAsStringAsync();
        logger.LogError("failed to resize image. Http Status: {statusCode}, Response: {responseContent}",
            responseMessage.StatusCode, responseContent);

        return new ApiResponse<byte[]?>(false, $"failed to get image info. Http Status: {responseMessage.StatusCode}, Response: {responseContent}", default);
    }
}
