# Get Logs

 This example demonstrates how to get GPS data (LogRecord) for a given vehicle (Device).

Steps:
1. Authenticate a user via login, password, database and server using the Geotab API object.
2. Search for a device by its serial number.
3. Get logs associated with the device for a given time period.

## Prerequisites
The sample application requires:

- [.Net core 2.0 SDK](https://dot.net/core) or higher

## Getting started

```
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd GetLogs
> dotnet run "my.geotab.com" "database" "user@email.com" "password" "G8SERIALNO"
```
