using System.Text.Json;

namespace ExperimentServer.Services;

public class ImageConversionApi(Config config, IHttpClientFactory httpClientFactory, ILogger<ImageConversionApi> logger)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(nameof(Config.ImageConversionApi));
    private readonly string url = $"{config.ImageConversionApi}/docs?format=Jpeg&quality=100";
    private const string FILE_NAME = "file.pdf";
    public async Task<string[]> ToImagesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(memoryStream.ToArray()), "file", FILE_NAME }
        };
        var responseMessage = await httpClient.PostAsync(url, content);
        var responseContent = await responseMessage.Content.ReadAsStringAsync();
        if (responseMessage.IsSuccessStatusCode)
        {
            var pdfImages = JsonSerializer.Deserialize<string[]>(responseContent);
            if (pdfImages is null)
            {
                logger.LogError("failed to convert pdf.");
                return [];
            }
            return pdfImages;
        }
        logger.LogError("failed to convert pdf. Http Status: {statusCode}, Response: {responseContent}",
            responseMessage.StatusCode, responseContent);
        return [];
    }
}
