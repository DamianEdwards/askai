#:package System.CommandLine@2.0.1

using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var urlOption = new Option<string>("--url") { Description = "The OpenAI endpoint URL", Required = true };
var keyOption = new Option<string?>("--key") { Description = "The authentication token" };
var keyEnvOption = new Option<string?>("--key-env") { Description = "The name of the environment variable containing the authentication token" };
var promptOption = new Option<string>("--prompt") { Description = "The prompt to send to the OpenAI API", Required = true };

var rootCommand = new RootCommand("A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(keyOption);
rootCommand.Options.Add(keyEnvOption);
rootCommand.Options.Add(promptOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var url = parseResult.GetValue(urlOption)!;
    var key = parseResult.GetValue(keyOption);
    var keyEnv = parseResult.GetValue(keyEnvOption);
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
    
    await SendPromptToOpenAI(url, actualKey, prompt);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task SendPromptToOpenAI(string url, string key, string prompt)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    
    var requestBody = new
    {
        model = "gpt-3.5-turbo",
        messages = new[]
        {
            new { role = "user", content = prompt }
        }
    };
    
    var jsonOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };
    var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
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
