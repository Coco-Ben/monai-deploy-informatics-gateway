

# MONAI Informatics Gateway

[![License](https://img.shields.io/badge/license-Apache%202.0-green.svg)](LICENSE)

## Build

### Prerequisites

* [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)


### Development Environment
During development, change any settings inside the `appsettings.Development.json` file.
First, export the following environment variable before executing `dotnet run`:

```bash
export DOTNET_ENVIRONMENT=Development
```

### Building MONAI Informatics Gateway

```bash
src$ dotnet build
```
