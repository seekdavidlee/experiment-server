﻿using Eklee.Reports.Client.Models;
using ExperimentServer.Models;
using ExperimentServer.Services;
using ExperimentServer.Shared;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ExperimentServer.Pages.Datasets;

public partial class DataSetDashboard
{
    private RadzenDataGrid<DataSetModel>? dataGrid;

    private readonly DataSetDashboardModel model = new();
    [Inject]
    private FileSystemApi? Client { get; set; }

    [Inject]
    private DialogService? DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        model.Items.Clear();
        model.Items.AddRange(await Client!.GetDataSetsAsync());
        await dataGrid!.Reload();
    }

    private async Task Edit(DataSetModel datasetModel)
    {
        bool? result = await DialogService!.OpenAsync<EditDataSetDialog>("Edit DataSet", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", datasetModel },
             { "Models", model.Items },
             { "Client", Client! }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "400px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }

    private async Task AddNew()
    {
        bool? result = await DialogService!.OpenAsync<EditDataSetDialog>("New DataSet", new Dictionary<string, object>
        {
             { "DialogService", DialogService },
             { "Model", new DataSetModel() },
             { "Models", model.Items },
             { "Client", Client! }
        },
        new DialogOptions
        {
            CloseDialogOnEsc = true,
            Width = "700px",
            Height = "400px",
        });

        if (result == true)
        {
            await RefreshAsync();
        }
    }
}
