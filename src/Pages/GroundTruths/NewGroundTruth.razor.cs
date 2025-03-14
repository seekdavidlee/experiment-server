using Eklee.Reports.Client.Models;
using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;

namespace ExperimentServer.Pages.GroundTruths;

public partial class NewGroundTruth
{
    private RadzenDataGrid<ImageInferenceResult>? dataGrid;

    [Inject]
    private FileSystemApi? FileSystemApi { get; set; }

    [Inject]
    private ImageConversionApi? ImageConversionApi { get; set; }


    [Parameter]
    public GroundTruthsDashboardModel? Model { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    private GroundTruthsDashboardModel model
    {
        get
        {
            return Model!;
        }
    }

    [Parameter]
    public Action? OnCancel { get; set; }

    [Parameter]
    public Action<GroundTruth>? OnSave { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var result = await FileSystemApi!.GetPromptsAsync();
        model.NewGroundTruthModel.UserPrompt = result.User;
        model.NewGroundTruthModel.SystemPrompt = result.System;
    }

    private async Task OpenImageAsync()
    {
        await DialogService!.OpenAsync<ShowImageDialog>(model.NewGroundTruthModel.ImageFilename, new Dictionary<string, object>
        {
            { "Base64", model.NewGroundTruthModel.Base64Images![PageCounter] }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "90%",
            Height = "90%",
        });
    }

    private int PageCounter;

    private async Task UpdatePromptsAsync()
    {
        var response = await FileSystemApi!.SavePromptsAsync(new Prompts(
               model.NewGroundTruthModel.SystemPrompt!,
               model.NewGroundTruthModel.UserPrompt!));

        await DialogService!.Alert(response.Success ? "Prompts are updated successfully." : $"Error: {response.ErrorMessage}", "Message");
    }

    private async Task InputFileChange(InputFileChangeEventArgs e)
    {
        if (Model is null)
        {
            return;
        }

        PageCounter = 0;

        try
        {
            // Get the file from the event
            var file = e.File;

            if (file.ContentType == "application/pdf")
            {
                model.NewGroundTruthModel.IsProcessing = true;
                StateHasChanged();
                var pdfImages = await ImageConversionApi!.ToImagesAsync(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                if (pdfImages.Length > 0)
                {
                    Model.NewGroundTruthModel.Base64Images = pdfImages.Select(x => $"data:image/jpg;base64,{x}").ToArray();
                }
                model.NewGroundTruthModel.IsProcessing = false;
                StateHasChanged();
            }

            if (file.ContentType == "image/jpg")
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // Limit to 10 MB
                var imageBytes = new byte[file.Size];
                await stream.ReadAsync(imageBytes);
                Model.NewGroundTruthModel.Base64Images = [$"data:{file.ContentType};base64,{Convert.ToBase64String(imageBytes)}"];
            }

            Model.NewGroundTruthModel.ImageFilename = file.Name;
            Model.NewGroundTruthModel.ImageFileContentType = file.ContentType;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Model.ErrorMessage = $"Error displaying file: {ex.Message}";
        }

        StateHasChanged();
    }

    private void CancelAdd()
    {
        DiscardImage();

        if (OnCancel is not null)
        {
            OnCancel();
        }
    }

    private async void DiscardImage()
    {
        if (Model is null)
        {
            return;
        }
        PageCounter = 0;
        Model.NewGroundTruthModel.Base64Images = null;
        Model.NewGroundTruthModel.ImageFilename = null;
        Model.NewGroundTruthModel.ImageFileContentType = null;
        Model.NewGroundTruthModel.InferenceResults.Clear();
        await dataGrid!.Reload();
    }

    private async Task ShowDetails(ImageInferenceResult result)
    {
        await DialogService!.OpenAsync<InferenceResultDetailsDialog>("Details", new Dictionary<string, object> { { "Model", result } },
            new DialogOptions
            {
                CloseDialogOnEsc = true,
                Width = "700px",
                Height = "512px",
            });
    }

    private async Task RunInferenceAsync()
    {
        //if (Model is null)
        //{
        //    return;
        //}

        //if (Model.NewGroundTruthModel.ImageBytes is null)
        //{
        //    return;
        //}

        //Model.ErrorMessage = null;
        //Model.NewGroundTruthModel.IsProcessing = true;

        //StateHasChanged();

        //try
        //{
        //    using var uploadStream = new MemoryStream(Model.NewGroundTruthModel.ImageBytes);

        //    var content = new MultipartFormDataContent();

        //    // Add the file content
        //    var fileContent = new StreamContent(uploadStream);
        //    fileContent.Headers.ContentType = new MediaTypeHeaderValue(Model.NewGroundTruthModel.ImageFileContentType!);
        //    content.Add(fileContent, "file", Model.NewGroundTruthModel.ImageFilename!);

        //    if (!string.IsNullOrEmpty(Model.NewGroundTruthModel.UserPrompt))
        //        content.Headers.Add("x-user-prompt", Model.NewGroundTruthModel.UserPrompt);

        //    // Post the content to the server
        //    var response = await HttpClient!.PostAsync("images/inference", content);

        //    // Handle the response
        //    if (response.IsSuccessStatusCode)
        //    {
        //        Model.NewGroundTruthModel.SelectedTabIndex = 1;
        //        var result = JsonSerializer.Deserialize<ImageInferenceResult>(await response.Content.ReadAsStringAsync(), Extensions.options)!;

        //        if (Model.NewGroundTruthModel.ImageBase64 is null && result.Base64PdfJpgImages is not null && result.Base64PdfJpgImages.Length > 0)
        //        {
        //            Model.NewGroundTruthModel.ImageBase64 = $"data:image/jpg;base64,{result.Base64PdfJpgImages[0]}";
        //        }

        //        Model.NewGroundTruthModel.InferenceResults.Add(result);
        //        Model.NewGroundTruthModel.Answer = result.Text;

        //        try
        //        {
        //            await HttpClient.PostAsync($"page-views/inference-results.{Model.NewGroundTruthModel.ImageFilename}.json",
        //                new StringContent(JsonSerializer.Serialize(Model.NewGroundTruthModel.InferenceResults)));
        //        }
        //        catch
        //        {
        //            // do nothing if fail to save, no big deal
        //        }

        //        await dataGrid!.Reload();
        //    }
        //    else
        //    {
        //        Model.ErrorMessage = $"Failed to upload file. Status code: {response.StatusCode}";
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Model.ErrorMessage = $"Error uploading file: {ex.Message}";
        //}
        //finally
        //{
        //    Model.NewGroundTruthModel.IsProcessing = false;
        //}

        StateHasChanged();
    }

    private void Save()
    {
        if (OnSave is not null)
        {
            OnSave(new GroundTruth
            {
                Id = model.NewGroundTruthModel.ImageFilename!,
                Question = JsonSerializer.Serialize(model.NewGroundTruthModel.Base64Images!),
                Answer = model.NewGroundTruthModel.Answer!,
                Type = Constants.ImageInferenceTypes.Receipts,
                Tags = [
                new GroundTruthTag
                {
                    Name = "AvgDuarationInSeconds",
                    Value = model.NewGroundTruthModel.InferenceResults.Select(x=>x.DurationInSeconds).Average().ToString()
                }]
            });
        }
        DiscardImage();
    }
}
