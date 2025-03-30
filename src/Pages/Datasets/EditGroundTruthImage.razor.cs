using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExperimentServer.Pages.Datasets;

public partial class EditGroundTruthImage
{
    [Parameter]
    public Guid DatasetId { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    [SupplyParameterFromQuery(Name = "action")]
    public string? Action { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private ImageConversionApi? ImageConversionApi { get; set; }

    [Inject]
    private InProgressIndicatorService? InProgressIndicator { get; set; }

    private string? ErrorMessage;

    private bool IsSaving { get; set; }

    private GroundTruthImage? Model;

    private int ImageZoomLevel { get; set; } = 100;

    private ImageInfo? ImageInfo { get; set; }

    private int ResizePercent { get; set; } = 50;

    private int CurrentIndex;
    private List<GroundTruthImage>? GroundTruthImages;

    protected override async Task OnInitializedAsync()
    {
        string modelsKey = $"ListOf{nameof(GroundTruthImage)}";
        Model = UserSession!.Items[nameof(GroundTruthImage)] as GroundTruthImage;
        GroundTruthImages = UserSession.Items.TryGetValue(modelsKey, out object? value) ? value as List<GroundTruthImage> : null;
        if (GroundTruthImages is not null)
        {
            CurrentIndex = GroundTruthImages.FindIndex(x => x.Id == Id);
        }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Model is not null && GroundTruthImages is not null)
        {
            InProgressIndicator!.Show("loading ground truth...");
            var result = await Client!.GetGroundTruthImageAsync(DatasetId, Model);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
            else if (result.Result.Length > 0)
            {
                var imageInfoResponse = await ImageConversionApi!.GetImageInfo(result.Result);
                if (imageInfoResponse.Success)
                {
                    ImageInfo = imageInfoResponse.Result;
                }

                base64Images = [$"data:image/jpg;base64,{Convert.ToBase64String(result.Result)}"];
                pageSize = base64Images!.Length;
                Base64Image = base64Images[pageCounter];
            }

            originalModelHash = ComputeHash(JsonSerializer.Serialize(Model));
            InProgressIndicator!.Hide();
        }
    }

    private async Task NextImage()
    {
        if (GroundTruthImages is not null && CurrentIndex < GroundTruthImages.Count)
        {
            CurrentIndex++;
            Model = GroundTruthImages[CurrentIndex];
            await LoadAsync();

        }
    }

    private async Task PreviousImage()
    {
        if (GroundTruthImages is not null && CurrentIndex > 0)
        {
            CurrentIndex--;
            Model = GroundTruthImages[CurrentIndex];
            await LoadAsync();
        }
    }

    private string? originalModelHash;
    private bool isImageChanged = false;

    private static string ComputeHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private const string SPLIT = ";base64,";
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        var currentModelHash = ComputeHash(JsonSerializer.Serialize(Model));
        bool isModelChanged = originalModelHash != currentModelHash;
        if (!isModelChanged && !isImageChanged)
        {
            // nothing has changed
            NavigationManager!.NavigateTo($"datasets/{DatasetId}");
            return;
        }

        if (Model!.Tags is not null)
        {
            foreach (var tag in Model.Tags)
            {
                if (string.IsNullOrEmpty(tag.Name) || string.IsNullOrEmpty(tag.Value))
                {
                    ErrorMessage = "please enter a valid tag name and value";
                    return;
                }
            }
        }

        if (base64Images.Length == 0)
        {
            ErrorMessage = "please select an image";
            return;
        }

        if (string.IsNullOrEmpty(Model!.DisplayName))
        {
            ErrorMessage = "please enter a valid display name";
            return;
        }

        foreach (var field in Model.Fields!)
        {
            if (string.IsNullOrEmpty(field.Value))
            {
                ErrorMessage = "please enter a valid field value";
                return;
            }
        }

        foreach (var image in GroundTruthImages!.Where(x => x.Id != Model.Id))
        {
            if (image.Fields is null) continue;

            var fieldsToMatch = image.Fields.Where(x => !x.IsSubjective).ToList();

            int matchCount = 0;
            List<string> matchFieldNames = [];
            foreach (var f in fieldsToMatch)
            {
                var match = Model.Fields.SingleOrDefault(x => x.Name == f.Name && x.Value == f.Value);
                if (match is null) continue;

                matchCount++;
                matchFieldNames.Add(f.Name!);
            }

            if (matchCount == fieldsToMatch.Count)
            {
                ErrorMessage = $"this ground truth image already exists with the following fields matched: {string.Join(',', matchFieldNames)}";
                return;
            }
        }

        var imageBase64 = base64Images![pageCounter];

        var index = imageBase64.IndexOf(SPLIT, StringComparison.Ordinal);
        var raw = imageBase64[(index + SPLIT.Length)..];

        if (isModelChanged && isImageChanged)
        {
            var result = await Client!.SaveGroundTruthImageAsync(DatasetId, Model!, Convert.FromBase64String(raw)!);
            if (result.Success)
            {
                NavigationManager!.NavigateTo($"datasets/{DatasetId}");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        else if (isModelChanged)
        {
            var result = await Client!.SaveGroundTruthImageAsync(DatasetId, Model!);
            if (result.Success)
            {
                NavigationManager!.NavigateTo($"datasets/{DatasetId}");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        else if (isImageChanged)
        {
            var result = await Client!.SaveGroundTruthImageAsync(DatasetId, Model.Id, Convert.FromBase64String(raw)!);
            if (result.Success)
            {
                NavigationManager!.NavigateTo($"datasets/{DatasetId}");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
    }

    private string? Base64Image { get; set; }
    private string[] base64Images = [];
    private int pageCounter;
    private int pageSize;
    private void Cancel()
    {
        NavigationManager!.NavigateTo($"datasets/{DatasetId}");
    }

    private void Previous()
    {
        if (pageCounter > 0)
        {
            pageCounter--;
            Base64Image = base64Images![pageCounter];
        }
    }

    private void Next()
    {
        if (pageCounter < pageSize)
        {
            pageCounter++;
            Base64Image = base64Images![pageCounter];
        }
    }

    private async Task InputFileChange(InputFileChangeEventArgs e)
    {
        if (Model is null)
        {
            return;
        }

        ErrorMessage = null;
        pageCounter = 0;
        pageSize = 0;

        try
        {
            InProgressIndicator!.Show("processing...");
            // Get the file from the event
            var file = e.File;
            GroundTruthTag? sourceImageSize = null;

            if (file.ContentType == "application/pdf")
            {

                var pdfImagesResponse = await ImageConversionApi!.ToImagesAsync(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                if (pdfImagesResponse.Success && pdfImagesResponse.Result.Length > 0)
                {
                    base64Images = pdfImagesResponse.Result.Select(x => $"data:image/jpg;base64,{x}").ToArray();

                    var imageInfoResponse = await ImageConversionApi!.GetImageInfo(Convert.FromBase64String(pdfImagesResponse.Result[0]));
                    if (imageInfoResponse.Success)
                    {
                        ImageInfo = imageInfoResponse.Result;
                        sourceImageSize = new GroundTruthTag { Name = "source_image_size", Value = $"width:{ImageInfo!.Width}, height:{ImageInfo.Height}" };
                    }
                }
                else
                {
                    ErrorMessage = pdfImagesResponse.ErrorMessage;
                    return;
                }
            }


            if (file.ContentType == "image/jpg" || file.ContentType == "image/jpeg")
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // Limit to 10 MB
                var imageBytes = new byte[file.Size];
                await stream.ReadAsync(imageBytes);
                base64Images = [$"data:{file.ContentType};base64,{Convert.ToBase64String(imageBytes)}"];

                var imageInfoResponse = await ImageConversionApi!.GetImageInfo(imageBytes);
                if (imageInfoResponse.Success)
                {
                    ImageInfo = imageInfoResponse.Result;
                    sourceImageSize = new GroundTruthTag { Name = "source_image_size", Value = $"width:{ImageInfo!.Width}, height:{ImageInfo.Height}" };
                }
            }

            Model.DisplayName = file.Name;
            var tags = Model.Tags is not null ? [.. Model.Tags] : new List<GroundTruthTag>();
            tags.Add(new GroundTruthTag { Name = "source_content_type", Value = file.ContentType });
            if (sourceImageSize is not null)
            {
                tags.Add(sourceImageSize);
            }

            Model.Tags = [.. tags];

            pageSize = base64Images!.Length;
            Base64Image = base64Images![pageCounter];

            isImageChanged = true;

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error displaying file: {ex.Message}";
        }
        finally
        {
            InProgressIndicator!.Hide();
        }

        StateHasChanged();
    }

    private void AddTag()
    {
        var tags = Model!.Tags is not null ? [.. Model.Tags] : new List<GroundTruthTag>();

        tags.Add(new GroundTruthTag());
        Model.Tags = [.. tags];
    }

    private async Task Resize()
    {
        var imageBase64 = base64Images![pageCounter];
        var index = imageBase64.IndexOf("base64,", StringComparison.Ordinal);
        var rawImage = imageBase64[(index + "base64,".Length)..];
        var result = await ImageConversionApi!.ResizeImageInfo(Convert.FromBase64String(rawImage), ResizePercent);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
        }
        else
        {
            var tags = Model!.Tags is not null ? [.. Model.Tags] : new List<GroundTruthTag>();
            if (ImageInfo is not null)
            {
                tags.Add(new GroundTruthTag { Name = "image_original_size", Value = $"width:{ImageInfo.Width}, height:{ImageInfo.Height}" });
            }

            ImageInfo = null;

            base64Images![pageCounter] = $"data:image/jpg;base64,{Convert.ToBase64String(result.Result!)}";
            Base64Image = base64Images![pageCounter];

            var imageInfoResponse = await ImageConversionApi!.GetImageInfo(result.Result!);
            if (imageInfoResponse.Success)
            {
                ImageInfo = imageInfoResponse.Result;
                tags.Add(new GroundTruthTag { Name = "image_new_size", Value = $"width:{ImageInfo!.Width}, height:{ImageInfo.Height}" });
            }
            Model.Tags = [.. tags];

            isImageChanged = true;
        }
    }

    private void RemoveTag(GroundTruthTag tag)
    {
        Model!.Tags = Model.Tags!.Where(x => x != tag).ToArray();
    }
}
