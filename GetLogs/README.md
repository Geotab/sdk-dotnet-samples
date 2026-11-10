# Get Logs

 This example demonstrates how to get GPS data (LogRecord) for a given vehicle (Device).

Steps:

1. Authenticate a user via login, password, database and server using the Geotab API object.
1. Search for a device by its serial number.
1. Get logs associated with the device for a given time period.

## Prerequisites

The sample application requires:

- [.NET SDK 10.0.114](https://dotnet.microsoft.com/download/dotnet/10.0) or later (runtime 10.0.14+, see CVE-2026-111497)

## Getting started

```shell
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd GetLogs
> dotnet run "my.geotab.com" "database" "user@email.com" "password" "G8SERIALNO"
```
