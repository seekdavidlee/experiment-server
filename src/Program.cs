using ExperimentServer;
using ExperimentServer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddRadzenComponents();

var httpClient = new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
};
var data = await httpClient.GetStringAsync("/Data/app.json");
var config = JsonSerializer.Deserialize<Config>(data)!;
builder.Services.AddSingleton(config);

builder.Services.AddHttpClient(nameof(Config.FileSystemApi), client =>
{
    client.BaseAddress = new Uri(config.ApiBaseUrl!);
    client.Timeout = TimeSpan.FromMinutes(8);
});

builder.Services.AddHttpClient(nameof(Config.InferenceApi), client =>
{
    client.BaseAddress = new Uri(config.InferenceApiBaseUrl!);
    client.Timeout = TimeSpan.FromMinutes(8);
});

builder.Services.AddHttpClient(nameof(Config.ImageConversionApi), client =>
{
    client.BaseAddress = new Uri(config.ApiBaseUrl!);
    client.Timeout = TimeSpan.FromMinutes(8);
});

builder.Services.AddScoped<FileSystemApi>();
builder.Services.AddScoped<ImageConversionApi>();
builder.Services.AddScoped<InferenceApi>();
builder.Services.AddSingleton<UserSession>();

await builder.Build().RunAsync();
