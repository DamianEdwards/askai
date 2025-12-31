# askai
A command-line tool that sends a user-provided prompt to an OpenAI endpoint and prints the response.

## Prerequisites

This tool is currently only available as source (via this repo) and requires the .NET 10 SDK to run.

.NET 10 is open-source and supports Windows, macOS, and Linux. Get it from https://get.dot.net.

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/DamianEdwards/askai.git
   cd askai
   ```

2. **Authenticate with GitHub** (choose one option)

   **Option A: Use the GitHub CLI (recommended)**
   
   If you have the [GitHub CLI](https://cli.github.com) installed, just authenticate with it:

   ```bash
   gh auth login
   ```

   The tool will automatically use your GitHub CLI token when calling GitHub Models.

   **Option B: Create a GitHub Personal Access Token (PAT)**
   
   - Go to [GitHub Settings > Developer settings > Personal access tokens > Fine-grained tokens](https://github.com/settings/tokens?type=beta)
   - Click **Generate new token**
   - Give your token a name (e.g., "askai")
   - Set an expiration date
   - Under **Permissions**, expand **Account permissions** and set **GitHub Copilot Chat** (or **Models**) to **Read-only**
   - Click **Generate token**
   - Copy the generated token (you won't be able to see it again)
   - Configure the token using user secrets:

     ```bash
     dotnet user-secrets --file askai.cs set key "YOUR_GITHUB_PAT_HERE"
     ```

3. **Run the tool**

   ```bash
   dotnet askai.cs "tell me a joke"
   ```

   You should see the AI's response!

## Usage

```bash
dotnet askai.cs "tell me a joke"
```

### Options

- `--url`: The OpenAI endpoint URL. Defaults to `https://models.github.ai/inference`
- `--key`: The authentication token (PAT for GitHub Models, API key for OpenAI, etc.). Optional when using GitHub Models with the GitHub CLI installed and authenticated
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

#### GitHub CLI Authentication

When using the default GitHub Models endpoint, the tool can automatically retrieve your authentication token from the [GitHub CLI](https://cli.github.com) if it's installed and you're logged in:

```bash
gh auth login
dotnet askai.cs "tell me a joke"
```

No additional configuration is needed! The tool will use `gh auth token` to get your token.

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

#### Response files

You can optionally provide any option or argument values via a response (.rsp) file. This can be particularly useful to pass a larger prompt, e.g. in automation scenarios. You can provide values for different options and the prompt argument from different sources, e.g. key from environment variable, prompt from a response file, e.g.:

```bash
echo "Imagine this prompt was built up and emitted by some more complex automation logic." > prompt.rsp
export ASKAI_KEY="your-api-key-here"
export ASKAI_MODEL="gpt-5.2"
dotnet askai.cs @prompt.rsp
```

Learn more about using response files in the [`System.CommandLine` documentation](https://learn.microsoft.com/dotnet/standard/commandline/syntax#response-files);

#### User Secrets

You can also use .NET user secrets when running a DEBUG build (e.g. from source):

```bash
dotnet user-secrets --file askai.cs set key "your-api-key-here"
```

### Examples

Using GitHub Models with GitHub CLI (no key needed):
```bash
dotnet askai.cs "tell me a joke"
```

Using GitHub Models with explicit key:
```bash
dotnet askai.cs --key YOUR_API_KEY "tell me a joke"
```

Using a specific model:
```bash
dotnet askai.cs --key YOUR_API_KEY --model gpt-5.2 "tell me a joke"
```

Using a custom model:
```bash
dotnet askai.cs --key YOUR_API_KEY --model custom --custom-model my-fine-tuned-model "tell me a joke"
```

Using a different endpoint:
```bash
dotnet askai.cs --url https://api.openai.com/v1 --key YOUR_API_KEY "tell me a joke"
```

With key configured via environment variable:
```bash
export ASKAI_KEY="your-api-key-here"
dotnet askai.cs "tell me a joke"
```

With diagnostic verbosity:
```bash
dotnet askai.cs -v "tell me a joke"
```

With minimal output (just the answer):
```bash
dotnet askai.cs --verbosity minimal "tell me a joke"
```

## Publishing from source

Publish the app from source to get a self-contained, native executable for the current platform in the `./artifacts` directory:

```bash
dotnet publish askai.cs
```

*NOTE: This requires the prerequisites for building native AOT binaries for your platform. See the [documentation for full details](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites).*

Now you can run the output directly:

```bash
cd ./artifacts/askai
askai "tell me a joke"
```

## Other resources

Learn more about working with [.NET 10 file-based apps via the documentation](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps).
