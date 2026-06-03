# ConfigWeave

Variable substitution for `Microsoft.Extensions.Configuration` with late binding and reload support.

Configuration values can reference other configuration values, environment variables, and use default fallbacks — all resolved at access time.

## Installation

```bash
dotnet add package ConfigWeave
```

## Quick Start

```json
{
  "Database": {
    "Host": "${@DB_HOST|localhost}",
    "Port": "${@DB_PORT|5432}",
    "ConnectionString": "Server=${Database:Host};Port=${Database:Port};Database=myapp"
  }
}
```

```csharp
using ConfigWeave;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build()
    .WithSubstitution();

var connectionString = config["Database:ConnectionString"];
// → "Server=localhost;Port=5432;Database=myapp"
```

## Syntax

| Reference | Description |
|-----------|-------------|
| `${Key}` | Config key reference |
| `${Section:Key}` | Nested config key |
| `${@ENV_VAR}` | Environment variable (read at access time) |
| `${Key\|default}` | Fallback if key is missing |
| `${@ENV_VAR\|default}` | Env var with fallback |
| `$${Key}` | Literal `${Key}` — escape with `$$` |

## ASP.NET Core

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config
            .AddJsonFile("appsettings.json", reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddSubstitution();
    });
```

## License

MIT