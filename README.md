# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.

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

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run -- --url <url> --key <key> --prompt <prompt>
```

