# Import Users

This console application demonstrates importing users from a .csv file.

## Steps:

1. Process command line arguments to specify Server, Database, Username, Password, and Input File, and load the .csv file.
2. Create a Geotab API object and authenticate.
3. Create users.
4. Add organization and security nodes to the users.
5. Import users into the database.

> **Note:** The provided .csv file is a sample. You may need to adjust entries (such as group names or password complexity) for the example to function correctly.

## Prerequisites

[.NET Core 2.0 SDK](https://dot.net/core) or higher

## CSV Layout

The .csv file format should follow this layout:

email | password | data access | security clearance name | first name | last name

```csv
# importUsers.csv
# Structure: User (Email), Password, Data Access,Security Clearance,First Name,Last Name
# -------------------------------------------------------------------------
# lines beginning with '#' are comments and ignored

# Basic authentication users
BasicUser@company.com,5bJknaJPKJSKP62Z,Entire Organization,Administrator,Basic,User
```

## Getting started

```shell
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd ImportUsers
> dotnet run "my.geotab.com" "database" "user@email.com" "password" "importUsers.csv"
```
