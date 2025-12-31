# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the API response.

## Usage

```bash
askai "tell me a joke"
```

### Options

- `--url`: The OpenAI endpoint URL. Defaults to `https://models.github.ai/inference`
- `--key`: The authentication token (PAT for GitHub Models, API key for OpenAI, etc.)
- `--model`: The model to use. Valid values: `gpt-5.2`, `gpt-5.2-pro`, `gpt-5.1`, `gpt-5`, `gpt-5-mini`, `gpt-5-nano`, `custom`. Defaults to `gpt-5-mini`
- `--custom-model`: The custom model name (required when `--model` is `custom`)
- `--verbosity`: Set the verbosity level. Valid values: `minimal`, `normal`, `detailed`, `diagnostic`. Defaults to `normal`
- `-v`: Shorthand for `--verbosity diagnostic`

### Verbosity Levels

| Level | Description |
|-------|-------------|
| `minimal` | Just prints the AI response |
| `normal` | Prints the question and answer (default) |
| `detailed` | Adds debug information to stderr |
| `diagnostic` | Adds full diagnostic information to stderr |

### Arguments

- `prompt` (required): The prompt to send to the OpenAI API

### Configuration

Options can be configured via environment variables, a JSON settings file, or user secrets. Command-line options take precedence over configuration values.

#### Environment Variables

Set environment variables with the `AskAI__` prefix:

```bash
export AskAI__Key="your-api-key-here"
export AskAI__Url="https://api.openai.com/v1"
export AskAI__Model="gpt-5.2"
```

#### Settings File

Create an `askai.settings.json` file in the same directory:

```json
{
  "AskAI": {
    "Key": "your-api-key-here",
    "Url": "https://models.github.ai/inference",
    "Model": "gpt-5-mini"
  }
}
```

#### User Secrets

You can also use .NET user secrets:

```bash
dotnet user-secrets --id askai set "AskAI:Key" "your-api-key-here"
```

### Examples

Using GitHub Models (default URL):
```bash
askai --key YOUR_API_KEY "tell me a joke"
```

Using a specific model:
```bash
askai --key YOUR_API_KEY --model gpt-5.2 "tell me a joke"
```

Using a custom model:
```bash
askai --key YOUR_API_KEY --model custom --custom-model my-fine-tuned-model "tell me a joke"
```

Using a different endpoint:
```bash
askai --url https://api.openai.com/v1 --key YOUR_API_KEY "tell me a joke"
```

With key configured via environment variable:
```bash
export AskAI__Key="your-api-key-here"
askai "tell me a joke"
```

With diagnostic verbosity:
```bash
askai -v "tell me a joke"
```

With minimal output (just the answer):
```bash
askai --verbosity minimal "tell me a joke"
```

## Running

```bash
dotnet run askai.cs -- "tell me a joke"
```




