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

Options can be configured via environment variables or a JSON settings file, (or user secrets when running a DEBUG build from source). Command-line options take precedence over configuration values.

#### Environment Variables

Set environment variables with the `ASKAI_` prefix:

```bash
export ASKAI_KEY="your-api-key-here"
export ASKAI_URL="https://api.openai.com/v1"
export ASKAI_MODEL="gpt-5.2"
```

#### Settings File

Create an `askai.settings.json` file in the same directory:

```json
{
  "key": "your-api-key-here",
  "url": "https://models.github.ai/inference",
  "model": "gpt-5-mini"
}
```

#### User Secrets

You can also use .NET user secrets when running a DEBUG build (e.g. from source):

```bash
dotnet user-secrets --file askai.cs set key "your-api-key-here"
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
export ASKAI_KEY="your-api-key-here"
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

## Running from source

```bash
dotnet askai.cs -- "tell me a joke"
```

## Publishing from source

Publish the app from source to get a self-contained, native executable for the current platform in the `./artifacts` directory:

```bash
dotnet publish askai.cs
```

*NOTE: This requires the prerequisites for building native AOT binaries for your platform. See the [documentation for full details](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites).*


