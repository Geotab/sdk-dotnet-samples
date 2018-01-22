# Import Users

This is a console example of importing users from a .csv file.

Steps:

1. Process command line arguments: Server, Database, Username, Password, Input File and load .csv file.
1. Create Geotab API object and Authenticate.
1. Create users.
1. Add organization and security nodes to the users.
1. Create Geotab API object and Authenticate.
1. Import users into database.

> the .csv file included in this project is a sample, you may need to change entries (such as group names or password complexity) for the example to work.

## Prerequisites

The sample application requires:

- [.Net core 2.0 SDK](https://dot.net/core) or higher

## CSV layout

email | password | data access | secuity cleance name | first name | last name

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
