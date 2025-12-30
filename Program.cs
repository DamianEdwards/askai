#:package System.CommandLine@2.0.1

using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var urlOption = new Option<string>("--url") { Description = "The OpenAI endpoint URL", Required = true };
var keyOption = new Option<string?>("--key") { Description = "The authentication token" };
var keyEnvOption = new Option<string?>("--key-env") { Description = "The name of the environment variable containing the authentication token" };
var modelOption = new Option<string?>("--model") { Description = "The model to use (gpt-5.2, gpt-5.2-pro, gpt-5.1, gpt-5, gpt-5-mini, gpt-5-nano, custom). Defaults to gpt-5-mini" };
var customModelOption = new Option<string?>("--custom-model") { Description = "The custom model name (required when --model is 'custom')" };
var promptOption = new Option<string>("--prompt") { Description = "The prompt to send to the OpenAI API", Required = true };

var rootCommand = new RootCommand("A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(keyOption);
rootCommand.Options.Add(keyEnvOption);
rootCommand.Options.Add(modelOption);
rootCommand.Options.Add(customModelOption);
rootCommand.Options.Add(promptOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var url = parseResult.GetValue(urlOption)!;
    var key = parseResult.GetValue(keyOption);
    var keyEnv = parseResult.GetValue(keyEnvOption);
    var model = parseResult.GetValue(modelOption) ?? "gpt-5-mini";
    var customModel = parseResult.GetValue(customModelOption);
    var prompt = parseResult.GetValue(promptOption)!;
    
    // Validate that exactly one of --key or --key-env is provided
    if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(keyEnv))
    {
        Console.Error.WriteLine("Error: Either --key or --key-env must be specified.");
        Environment.Exit(1);
    }
    
    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(keyEnv))
    {
        Console.Error.WriteLine("Error: Cannot specify both --key and --key-env.");
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
    
    // Get the actual key value
    string actualKey;
    if (!string.IsNullOrEmpty(keyEnv))
    {
        actualKey = Environment.GetEnvironmentVariable(keyEnv) ?? string.Empty;
        if (string.IsNullOrEmpty(actualKey))
        {
            Console.Error.WriteLine($"Error: Environment variable '{keyEnv}' is not set or is empty.");
            Environment.Exit(1);
        }
    }
    else
    {
        actualKey = key!;
    }
    
    // Get the actual model name
    string actualModel = model == "custom" ? customModel! : model;
    
    await SendPromptToOpenAI(url, actualKey, actualModel, prompt);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task SendPromptToOpenAI(string url, string key, string model, string prompt)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    
    var requestBody = new ChatCompletionRequest
    {
        Model = model,
        Messages = new[]
        {
            new ChatMessage { Role = "user", Content = prompt }
        }
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
        
        Console.WriteLine(responseContent);
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

// JSON source generator context for native AOT support
[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ChatMessage[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
