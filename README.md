# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.

This is a [file-based .NET app](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps) that uses the `#:package` directive for package references.

## Usage

```bash
askai --url <url> --key <key> --prompt <prompt>
```

### Options

- `--url` (required): The OpenAI endpoint URL (e.g., `https://api.openai.com/v1`)
- `--key` (required): The authentication token for the OpenAI API
- `--prompt` (required): The prompt to send to the OpenAI API

### Example

```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY --prompt "tell me a joke"
```

## Running

```bash
dotnet run Program.cs -- --url <url> --key <key> --prompt <prompt>
```


