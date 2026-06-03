# ConfigWeave

ConfigWeave is a .NET library that extends the Microsoft.Extensions.Configuration API with variable substitution. Configuration values can reference other configuration values, environment variables, and use default fallbacks — all resolved at access time (late binding).

## Table of Contents

- [Basic Syntax](#basic-syntax)
- [Path Syntax](#path-syntax)
- [String Interpolation](#string-interpolation)
- [Data Types](#data-types)
- [Default Values](#default-values)
- [Environment Variables](#environment-variables)
- [Escaping](#escaping)
- [Error Handling](#error-handling)
- [API Usage](#api-usage)
- [Best Practices](#best-practices)

---

## Basic Syntax

Use `${KeyName}` to reference another configuration value:

```json
{
  "Host": "localhost",
  "ConnectionString": "Server=${Host};Database=mydb"
}
```

**Result:** `ConnectionString` resolves to `"Server=localhost;Database=mydb"`

---

## Path Syntax

Reference keys in nested sections using `:` as the path separator:

```json
{
  "Database": {
    "Host": "localhost",
    "Port": "5432",
    "ConnectionString": "Server=${Database:Host};Port=${Database:Port}"
  },
  "Logging": {
    "DatabaseHost": "${Database:Host}"
  }
}
```

**Results:**
- `Database:ConnectionString` → `"Server=localhost;Port=5432"`
- `Logging:DatabaseHost` → `"localhost"`

References always use the full path from the root. There is no implicit scope or relative lookup — what you write is what gets resolved.

---

## String Interpolation

A value can contain multiple references mixed with literal text:

```json
{
  "App": {
    "Name": "MyApp",
    "Version": "1.0.0",
    "FullName": "${App:Name} v${App:Version}",
    "Description": "Welcome to ${App:FullName}!"
  },
  "Server": {
    "Host": "localhost",
    "Port": "8080",
    "Url": "http://${Server:Host}:${Server:Port}"
  }
}
```

**Results:**
- `App:FullName` → `"MyApp v1.0.0"`
- `App:Description` → `"Welcome to MyApp v1.0.0!"` (references resolve recursively)
- `Server:Url` → `"http://localhost:8080"`

---

## Data Types

### Strings
String values support full substitution and interpolation as shown above.

### Numbers and Booleans
Non-string JSON values are converted to strings when substituted:

```json
{
  "MaxConnections": 100,
  "IsEnabled": true,
  "Message": "Max connections: ${MaxConnections}",
  "Status": "Enabled: ${IsEnabled}"
}
```

**Results:**
- `Message` → `"Max connections: 100"`
- `Status` → `"Enabled: true"`

### Arrays
Array elements are referenced by index:

```json
{
  "Servers": ["server1.com", "server2.com", "server3.com"],
  "PrimaryServer": "${Servers:0}",
  "BackupServer": "${Servers:1}"
}
```

**Results:**
- `PrimaryServer` → `"server1.com"`
- `BackupServer` → `"server2.com"`

---

## Default Values

Use `|` to provide a fallback if the referenced key does not exist:

```json
{
  "Port": "${SERVER_PORT|8080}",
  "Host": "${SERVER_HOST|localhost}"
}
```

**Results:**
- `Port` → `"8080"` (if `SERVER_PORT` is not defined)
- `Host` → `"localhost"` (if `SERVER_HOST` is not defined)

Without a default, referencing a missing key throws a `ConfigurationResolutionException`.

---

## Environment Variables

Prefix a name with `@` inside `${...}` to read directly from the process environment:

```json
{
  "Database": {
    "Host": "${@DB_HOST|localhost}",
    "Password": "${@DB_PASSWORD}"
  },
  "ApiKey": "${@API_KEY|default-dev-key}"
}
```

The `@` prefix is unambiguous — config key paths never start with `@`. A reference like `${env:Key}` is a normal config path lookup (section `env`, key `Key`), while `${@Key}` reads from the environment.

Environment variables are read on every access, so changes to the process environment are reflected immediately without reloading configuration. This also guarantees the value comes from the environment and cannot be silently overridden by a configuration file.

### Case Sensitivity

Config key references are **case-insensitive**, following standard .NET configuration conventions:

```json
{
  "MyKey": "Value",
  "Test1": "${mykey}",
  "Test2": "${MYKEY}",
  "Test3": "${MyKey}"
}
```

All three resolve to `"Value"`. Environment variable names in `${@...}` follow OS conventions (case-sensitive on Linux).

---

## Escaping

To include a literal `${...}` without substitution, prefix the `$` with another `$`:

```json
{
  "Key1": "Value1",
  "Key2": "$${Key1}",
  "Docs": "Use $${VariableName} syntax to reference a value"
}
```

**Results:**
- `Key2` → `"${Key1}"` (literal, not resolved)
- `Docs` → `"Use ${VariableName} syntax to reference a value"`

An unclosed `${` throws a `ConfigurationParseException`. If you need a literal `${` in a value, escape it with `$$`: `$${...}`.

---

## Error Handling

### Missing Keys
Referencing a key that does not exist throws `ConfigurationResolutionException`:

```
ConfigurationResolutionException: Referenced key 'NonExistentKey' not found.
```

Use a [default value](#default-values) to avoid this.

### Circular References
Circular references are detected and throw `CircularReferenceException` (a subclass of `ConfigurationResolutionException`):

```json
{
  "Key1": "${Key2}",
  "Key2": "${Key1}"
}
```

```
CircularReferenceException: Circular reference detected: Key1 -> Key2 -> Key1
```

### Catching Exceptions

```csharp
try
{
    var value = config["Key"];
}
catch (CircularReferenceException ex)
{
    Console.WriteLine($"Circular reference: {ex.ReferencePath}");
}
catch (ConfigurationResolutionException ex)
{
    Console.WriteLine($"Resolution error: {ex.Message}");
}
```

---

## API Usage

### Installation

```bash
dotnet add package ConfigWeave
```

### Basic Setup

Wrap a built `IConfigurationRoot` with `.WithSubstitution()`:

```csharp
using Microsoft.Extensions.Configuration;
using ConfigWeave;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build()
    .WithSubstitution();

var connectionString = config["Database:ConnectionString"];
```

Substitution is resolved on every access (late binding). The underlying configuration sources — including environment variables — are always read fresh.

### ASP.NET Core Integration

Use `AddSubstitution()` in `ConfigureAppConfiguration`:

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddSubstitution();
    })
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
```

### Options

```csharp
config.AddSubstitution(new SubstitutionOptions
{
    // What to do when a referenced key is not found and no default is provided.
    // Default: Throw
    UnresolvedKeyBehavior = UnresolvedKeyBehavior.Throw,

    // Maximum substitution depth before a CircularReferenceException is thrown.
    // Default: 10
    MaxRecursionDepth = 10
});
```

```csharp
public enum UnresolvedKeyBehavior
{
    Throw,        // Throw ConfigurationResolutionException
    ReturnNull,   // Return null
    KeepPattern   // Return the original "${Key}" string unchanged
}
```

### Reload Support

Because substitution is resolved at access time, reload works without any extra configuration:

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build()
    .WithSubstitution();

// Forward reload tokens from the underlying configuration
ChangeToken.OnChange(
    () => config.GetReloadToken(),
    () => Console.WriteLine("Configuration changed"));
```

When the underlying source reloads, the next access automatically resolves against the new values.

---

## Best Practices

**Use full paths for references.** Explicit paths make configuration easier to understand and refactor.

```json
{
  "Database": {
    "Host": "localhost"
  },
  "Cache": {
    "Host": "${Database:Host}"
  }
}
```

**Use environment variables for secrets and environment-specific values.**

```json
{
  "Database": {
    "Password": "${@DB_PASSWORD}",
    "Host": "${@DB_HOST|localhost}"
  }
}
```

**Always provide defaults for optional settings.** This makes local development work without setting environment variables.

```json
{
  "Server": {
    "Port": "${@PORT|8080}",
    "LogLevel": "${@LOG_LEVEL|Information}"
  }
}
```

**Keep reference chains short.** One or two levels of indirection (`A` references `B`, `B` references `C`) is fine. Deeper chains make configuration harder to trace and increase the chance of circular reference errors.

---

## Example: Full Application Configuration

```json
{
  "App": {
    "Name": "MyApplication",
    "Version": "1.0.0",
    "DisplayName": "${App:Name} v${App:Version}"
  },
  "Database": {
    "Host": "${@DB_HOST|localhost}",
    "Port": "${@DB_PORT|5432}",
    "Name": "mydb",
    "Username": "${@DB_USER|admin}",
    "Password": "${@DB_PASSWORD}",
    "ConnectionString": "Host=${Database:Host};Port=${Database:Port};Database=${Database:Name};Username=${Database:Username};Password=${Database:Password}"
  },
  "Redis": {
    "Host": "${@REDIS_HOST|localhost}",
    "Port": "${@REDIS_PORT|6379}",
    "ConnectionString": "${Redis:Host}:${Redis:Port}"
  },
  "Logging": {
    "Level": "${@LOG_LEVEL|Information}",
    "Path": "/var/log/${App:Name}.log"
  }
}
```

## Example: Multi-Environment Configuration

**appsettings.json**
```json
{
  "BaseUrl": "https://api.example.com",
  "Endpoints": {
    "Users": "${BaseUrl}/users",
    "Products": "${BaseUrl}/products",
    "Orders": "${BaseUrl}/orders"
  }
}
```

**appsettings.Development.json**
```json
{
  "BaseUrl": "http://localhost:5000"
}
```

The development override replaces `BaseUrl`, and all `Endpoints` references resolve to the local URL automatically on next access.
