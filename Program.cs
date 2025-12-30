#:package System.CommandLine@2.0.1

using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var urlOption = new Option<string>("--url") { Description = "The OpenAI endpoint URL", Required = true };
var keyOption = new Option<string>("--key") { Description = "The authentication token", Required = true };
var promptOption = new Option<string>("--prompt") { Description = "The prompt to send to the OpenAI API", Required = true };

var rootCommand = new RootCommand("A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(keyOption);
rootCommand.Options.Add(promptOption);

rootCommand.SetAction(parseResult =>
{
    var url = parseResult.GetValue(urlOption);
    var key = parseResult.GetValue(keyOption);
    var prompt = parseResult.GetValue(promptOption);
    
    if (url != null && key != null && prompt != null)
    {
        SendPromptToOpenAI(url, key, prompt).Wait();
    }
});

return rootCommand.Parse(args).Invoke();

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
    
    var jsonContent = JsonSerializer.Serialize(requestBody);
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
