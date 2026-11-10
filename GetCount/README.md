# Get Count

 This example demonstrates how to get a count the number of active vehicles, trailers, and zones in a database using the MyGeotab API.
 This examples also demonstrates how to use searches using the MyGeptab API.

Steps:

1. Create API from command line arguments.
1. Authenticate the user.
1. Get the count of active vehicles, trailers, and zones.

## Prerequisites

The sample application requires:

- [.NET SDK 10.0.114](https://dotnet.microsoft.com/download/dotnet/10.0) or later (runtime 10.0.14+, see CVE-2026-111497)

## Getting started

```shell
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd GetCount
> dotnet run "my.geotab.com" "database" "user@email.com" "password"
```
