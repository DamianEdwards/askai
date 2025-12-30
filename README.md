# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.

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
- `--model`: The model to use. Valid values: `gpt-5.2`, `gpt-5.2-pro`, `gpt-5.1`, `gpt-5`, `gpt-5-mini`, `gpt-5-nano`, `custom`. Defaults to `gpt-5-mini`
- `--custom-model`: The custom model name (required when `--model` is `custom`)
- `--prompt` (required): The prompt to send to the OpenAI API

### Examples

Using a direct API key with default model:
```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY --prompt "tell me a joke"
```

Using a specific model:
```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY --model gpt-5.2 --prompt "tell me a joke"
```

Using a custom model:
```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY --model custom --custom-model my-fine-tuned-model --prompt "tell me a joke"
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




