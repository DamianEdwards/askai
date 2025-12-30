# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.

This is a [file-based .NET app](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps) that uses the `#:package` directive for package references.

## Usage

```bash
askai --url <url> --key <key> --prompt <prompt>
# OR
askai --url <url> --key-env <env-var-name> --prompt <prompt>
```

### Options

- `--url` (required): The OpenAI endpoint URL (e.g., `https://api.openai.com/v1`)
- `--key`: The authentication token for the OpenAI API (one of `--key` or `--key-env` is required)
- `--key-env`: The name of the environment variable containing the authentication token (one of `--key` or `--key-env` is required)
- `--prompt` (required): The prompt to send to the OpenAI API

### Examples

Using a direct API key:
```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY --prompt "tell me a joke"
```

Using an environment variable for the API key:
```bash
export OPENAI_API_KEY="your-api-key-here"
askai --url https://api.openai.com/v1 --key-env OPENAI_API_KEY --prompt "tell me a joke"
```

## Running

```bash
dotnet run Program.cs -- --url <url> --key <key> --prompt <prompt>
# OR
dotnet run Program.cs -- --url <url> --key-env <env-var-name> --prompt <prompt>
```



