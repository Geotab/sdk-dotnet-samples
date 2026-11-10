# Get Fuel Tax Details (IFTA)

 This example demonstrates how to get fuel tax details (IFTA) for all vehilces in a time interval.

Steps:

1. Create API from command line arguments.
1. Authenticate the user.
1. Iterate through a list of devices.
1. Retrieve fuel tax details for each device over a given time interval.
1. Trim each detail to the time interval.

## Prerequisites

The sample application requires:

- [.NET SDK 10.0.114](https://dotnet.microsoft.com/download/dotnet/10.0) or later (runtime 10.0.14+, see CVE-2026-111497)

## Getting started

```shell
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd GetFuelTaxDetails
> dotnet run "database" "user@email.com" "password"
```
