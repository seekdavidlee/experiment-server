using ExperimentServer.Models;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ExperimentServer.Pages.Datasets;

public partial class EditGroundTruthImage
{
    [Parameter]
    public Guid DatasetId { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [Inject]
    private UserSession? UserSession { get; set; }

    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private ImageConversionApi? ImageConversionApi { get; set; }

    private string? ErrorMessage;

    private bool IsSaving { get; set; }

    private GroundTruthImage? Model;

    protected override async Task OnInitializedAsync()
    {
        Model = UserSession!.Items[nameof(GroundTruthImage)] as GroundTruthImage;

        if (Model is not null)
        {
            var result = await Client!.GetGroundTruthImageAsync(DatasetId, Model);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
            else if (result.Result.Length > 0)
            {
                base64Images = [$"data:image/jpg;base64,{Convert.ToBase64String(result.Result)}"];
                pageSize = base64Images!.Length;
                Base64Image = base64Images[pageCounter];
            }
        }
    }

    private const string SPLIT = ";base64,";
    private async Task SaveAsync()
    {
        ErrorMessage = null;
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

        var imageBase64 = base64Images![pageCounter];

        var index = imageBase64.IndexOf(SPLIT, StringComparison.Ordinal);
        var raw = imageBase64[(index + SPLIT.Length)..];

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
            // Get the file from the event
            var file = e.File;

            if (file.ContentType == "application/pdf")
            {
                //IsProcessing = true;
                StateHasChanged();
                var pdfImages = await ImageConversionApi!.ToImagesAsync(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                if (pdfImages.Length > 0)
                {
                    base64Images = pdfImages.Select(x => $"data:image/jpg;base64,{x}").ToArray();

                }
                //IsProcessing = false;
                StateHasChanged();
            }

            if (file.ContentType == "image/jpg" || file.ContentType == "image/jpeg")
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // Limit to 10 MB
                var imageBytes = new byte[file.Size];
                await stream.ReadAsync(imageBytes);
                base64Images = [$"data:{file.ContentType};base64,{Convert.ToBase64String(imageBytes)}"];
            }

            Model.DisplayName = file.Name;
            var tags = Model.Tags is not null ? [.. Model.Tags] : new List<GroundTruthTag>();
            tags.Add(new GroundTruthTag { Name = nameof(file.ContentType), Value = file.ContentType });

            pageSize = base64Images!.Length;
            Base64Image = base64Images![pageCounter];

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error displaying file: {ex.Message}";
        }

        StateHasChanged();
    }
}
