#:package System.CommandLine@2.0.1
#:package Microsoft.Extensions.Configuration@10.0.1
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@10.0.1
#:package Microsoft.Extensions.Configuration.Json@10.0.1
#:package Microsoft.Extensions.Configuration.UserSecrets@10.0.1
#:package Microsoft.Extensions.Logging@10.0.1
#:package Microsoft.Extensions.Logging.Console@10.0.1
#:property VersionPrefix=0.0.1

using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Build configuration from standard sources
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("askai.settings.json", optional: true)
    .AddEnvironmentVariables("ASKAI_");

#if DEBUG
configurationBuilder.AddUserSecrets<Program>(optional: true);
#endif

var configuration = configurationBuilder.Build();

var urlOption = new Option<string>("--url") { Description = "The OpenAI API endpoint URL. Defaults to GitHub Models. [env: ASKAI_URL]", DefaultValueFactory = _ => configuration["url"] ?? "https://models.github.ai/inference" };
var keyOption = new Option<string?>("--key") { Description = "The authentication token, e.g. PAT for GitHub models, API key for OpenAI, etc. [env: ASKAI_KEY]", DefaultValueFactory = _ => configuration["key"] };
var modelOption = new Option<string>("--model") { Description = "The model to use. Defaults to gpt-5-mini. [env: ASKAI_MODEL]", DefaultValueFactory = _ => configuration["model"] ?? "gpt-5-mini" };
var validModels = new[] { "gpt-5.2", "gpt-5.2-pro", "gpt-5.1", "gpt-5", "gpt-5-mini", "gpt-5-nano", "custom" };
modelOption.CompletionSources.Add(validModels);
modelOption.Validators.Add(result =>
{
    var value = result.GetValueOrDefault<string>();
    if (value is not null && !validModels.Contains(value))
    {
        result.AddError($"Invalid model '{value}'. Valid values are: {string.Join(", ", validModels)}");
    }
});
var customModelOption = new Option<string?>("--custom-model") { Description = "The custom model name (required when --model is 'custom'). [env: ASKAI_CUSTOMMODEL]", DefaultValueFactory = _ => configuration["custommodel"] };
var verbosityOption = new Option<Verbosity>("--verbosity") { Description = "Set the verbosity level", DefaultValueFactory = _ => Verbosity.Normal };
verbosityOption.CompletionSources.Add(["m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic"]);
var verbosityShortOption = new Option<bool>("-v") { Description = "Enable diagnostic verbosity (shorthand for --verbosity diagnostic)" };
var promptArgument = new Argument<string>("prompt") { Description = "The prompt to send to the OpenAI API" };

var rootCommand = new RootCommand("A command-line tool that sends a user-provided prompt to an OpenAI API endpoint and prints the response.");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(keyOption);
rootCommand.Options.Add(modelOption);
rootCommand.Options.Add(customModelOption);
rootCommand.Options.Add(verbosityOption);
rootCommand.Options.Add(verbosityShortOption);
rootCommand.Arguments.Add(promptArgument);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var url = parseResult.GetValue(urlOption)!;
    var key = parseResult.GetValue(keyOption);
    var model = parseResult.GetValue(modelOption)!;
    var customModel = parseResult.GetValue(customModelOption);
    var verbosity = parseResult.GetValue(verbosityShortOption) ? Verbosity.Diagnostic : parseResult.GetValue(verbosityOption);
    var prompt = parseResult.GetValue(promptArgument)!;
    
    // Create logger based on verbosity
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        builder.SetMinimumLevel(verbosity switch
        {
            Verbosity.Diagnostic => LogLevel.Trace,
            Verbosity.Detailed => LogLevel.Debug,
            _ => LogLevel.None
        });
    });
    var logger = loggerFactory.CreateLogger("askai");
    
    // Determine if we're using GitHub Models endpoint
    var isGitHubModels = url.Contains("models.github.ai", StringComparison.OrdinalIgnoreCase);
    
    // Get authentication key
    if (string.IsNullOrEmpty(key))
    {
        if (isGitHubModels)
        {
            // Try to get token from gh CLI
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("No key provided, attempting to get token from gh CLI");
            key = await TryGetGitHubCliTokenAsync(logger);
            
            if (string.IsNullOrEmpty(key))
            {
                // gh CLI not available or not authenticated
                var ghAvailable = await IsGitHubCliAvailableAsync();
                if (ghAvailable)
                {
                    Console.Error.WriteLine("Error: Not authenticated with GitHub CLI.");
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Please run 'gh auth login' to authenticate, or provide a token via --key or ASKAI_KEY.");
                }
                else
                {
                    Console.Error.WriteLine("Error: No authentication token provided.");
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Options:");
                    Console.Error.WriteLine("  1. Install the GitHub CLI (https://cli.github.com) and run 'gh auth login'");
                    Console.Error.WriteLine("  2. Provide a GitHub Personal Access Token via --key or ASKAI_KEY environment variable");
                }
                Environment.Exit(1);
            }
            
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Successfully retrieved token from gh CLI");
        }
        else
        {
            Console.Error.WriteLine($"Error: {keyOption.Name} must be specified (or set via ASKAI_KEY environment variable).");
            Environment.Exit(1);
        }
    }
    
    // Validate model option
    if (!validModels.Contains(model))
    {
        Console.Error.WriteLine($"Error: Invalid model '{model}'. Valid values are: {string.Join(", ", validModels)}");
        Environment.Exit(1);
    }
    
    // Validate custom model option
    if (model == "custom" && string.IsNullOrEmpty(customModel))
    {
        Console.Error.WriteLine($"Error: {customModelOption.Name} must be specified when {modelOption.Name} is 'custom'.");
        Environment.Exit(1);
    }
    
    if (model != "custom" && !string.IsNullOrEmpty(customModel))
    {
        Console.Error.WriteLine($"Error: {customModelOption.Name} can only be specified when {modelOption.Name} is 'custom'.");
        Environment.Exit(1);
    }
    
    // Get the actual model name
    var actualModel = model == "custom" ? customModel! : model;
    
    await SendPromptToOpenAI(url, key, actualModel, prompt, verbosity, logger, cancellationToken);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task SendPromptToOpenAI(string url, string key, string model, string prompt, Verbosity verbosity, ILogger logger, CancellationToken cancellationToken)
{
    if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Starting request to OpenAI API");
    if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("URL: {Url}", url);
    if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Model: {Model}", model);
    
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Authorization header set");
    
    var requestBody = new ChatCompletionRequest
    {
        Model = model,
        Messages = [new() { Role = "user", Content = prompt }]
    };
    
    var jsonContent = JsonSerializer.Serialize(requestBody, AppJsonSerializerContext.Default.ChatCompletionRequest);
    if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Request body: {RequestBody}", jsonContent);
    
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    var endpoint = url.TrimEnd('/') + "/chat/completions";
    if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Sending POST request to {Endpoint}", endpoint);
    
    try
    {
        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
        if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Response status: {StatusCode}", response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Response body: {ResponseBody}", responseContent);
        
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
        
        // Output based on verbosity level
        if (verbosity >= Verbosity.Normal)
        {
            Console.WriteLine($"Q: {prompt}");
            Console.WriteLine();
            Console.WriteLine($"A: {messageContent}");
        }
        else
        {
            // Minimal - just the answer
            Console.WriteLine(messageContent);
        }
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Operation cancelled.");
        Environment.Exit(1);
    }
    catch (Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "Request failed with exception");
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}

static async Task<bool> IsGitHubCliAvailableAsync()
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        if (process is null) return false;
        
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static async Task<string?> TryGetGitHubCliTokenAsync(ILogger logger)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = "auth token",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        if (process is null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Failed to start gh process");
            return null;
        }
        
        var token = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("gh auth token failed: {Error}", error);
            return null;
        }
        
        return token.Trim();
    }
    catch (Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "Failed to get token from gh CLI");
        return null;
    }
}

// Verbosity levels (Quiet not supported as it doesn't make sense for this tool)
enum Verbosity
{
    Minimal,    // M - Just the answer (default)
    Normal,     // N - Question and answer
    Detailed,   // D - Additional debug info to stderr
    Diagnostic  // Diag - Full diagnostic info to stderr
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
