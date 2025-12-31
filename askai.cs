#:package System.CommandLine@2.0.1
#:package Microsoft.Extensions.Configuration@10.0.1
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@10.0.1
#:package Microsoft.Extensions.Configuration.Json@10.0.1
#:package Microsoft.Extensions.Configuration.UserSecrets@10.0.1

using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

// Build configuration from standard sources
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddJsonFile("askai.settings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var configSection = configuration.GetSection("AskAI");

var urlOption = new Option<string>("--url") { Description = "The OpenAI endpoint URL", DefaultValueFactory = _ => configSection["Url"] ?? "https://models.github.ai/inference" };
var keyOption = new Option<string?>("--key") { Description = "The authentication token", DefaultValueFactory = _ => configSection["Key"] };
var modelOption = new Option<string?>("--model") { Description = "The model to use. Defaults to gpt-5-mini", DefaultValueFactory = _ => configSection["Model"] };
modelOption.CompletionSources.Add(["gpt-5.2", "gpt-5.2-pro", "gpt-5.1", "gpt-5", "gpt-5-mini", "gpt-5-nano", "custom"]);
var customModelOption = new Option<string?>("--custom-model") { Description = "The custom model name (required when --model is 'custom')", DefaultValueFactory = _ => configSection["CustomModel"] };
var promptArgument = new Argument<string>("prompt") { Description = "The prompt to send to the OpenAI API" };

var rootCommand = new RootCommand("A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(keyOption);
rootCommand.Options.Add(modelOption);
rootCommand.Options.Add(customModelOption);
rootCommand.Arguments.Add(promptArgument);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var url = parseResult.GetValue(urlOption)!;
    var key = parseResult.GetValue(keyOption);
    var model = parseResult.GetValue(modelOption) ?? "gpt-5-mini";
    var customModel = parseResult.GetValue(customModelOption);
    var prompt = parseResult.GetValue(promptArgument)!;
    
    // Validate that --key is provided
    if (string.IsNullOrEmpty(key))
    {
        Console.Error.WriteLine("Error: --key must be specified (or set via AskAI__Key environment variable).");
        Environment.Exit(1);
    }
    
    // Validate model option
    var validModels = new[] { "gpt-5.2", "gpt-5.2-pro", "gpt-5.1", "gpt-5", "gpt-5-mini", "gpt-5-nano", "custom" };
    if (!validModels.Contains(model))
    {
        Console.Error.WriteLine($"Error: Invalid model '{model}'. Valid values are: {string.Join(", ", validModels)}");
        Environment.Exit(1);
    }
    
    // Validate custom model option
    if (model == "custom" && string.IsNullOrEmpty(customModel))
    {
        Console.Error.WriteLine("Error: --custom-model must be specified when --model is 'custom'.");
        Environment.Exit(1);
    }
    
    if (model != "custom" && !string.IsNullOrEmpty(customModel))
    {
        Console.Error.WriteLine("Error: --custom-model can only be specified when --model is 'custom'.");
        Environment.Exit(1);
    }
    
    // Get the actual model name
    string actualModel = model == "custom" ? customModel! : model;
    
    await SendPromptToOpenAI(url, key, actualModel, prompt);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task SendPromptToOpenAI(string url, string key, string model, string prompt)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    
    var requestBody = new ChatCompletionRequest
    {
        Model = model,
        Messages = [ new() { Role = "user", Content = prompt } ]
    };
    
    var jsonContent = JsonSerializer.Serialize(requestBody, AppJsonSerializerContext.Default.ChatCompletionRequest);
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    var endpoint = url.TrimEnd('/') + "/chat/completions";
    
    try
    {
        var response = await httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine(responseContent);
            Environment.Exit(1);
        }
        
        var chatResponse = JsonSerializer.Deserialize(responseContent, AppJsonSerializerContext.Default.ChatCompletionResponse);
        var messageContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        
        if (messageContent is null)
        {
            Console.Error.WriteLine("Error: No content in response.");
            Environment.Exit(1);
        }
        
        Console.WriteLine(messageContent);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}

// Models for JSON serialization
class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }
    
    [JsonPropertyName("messages")]
    public required ChatMessage[] Messages { get; set; }
}

class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

// Response models
class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public ChatChoice[]? Choices { get; set; }
}

class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}

// JSON source generator context for native AOT support
[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ChatMessage[]))]
[JsonSerializable(typeof(ChatChoice[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
