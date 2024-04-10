# Import Zones from Shape File

This is a console example of importing complex zones from a shape or csv file.

Steps:

1. Process command line arguments: Server, Database, Username, Password, Options and load the specified shape file set/csv.
2. Process the shapes present in the shape file, using the Geotab.Geographical components to create zones from the shape points and names.
3. Create Geotab API object and Authenticate.
4. Import created zones into the database.

## Prerequisites

The sample application requires:

- [.Net core 2.0 SDK](https://dot.net/core) or higher

## Getting started

```shell
> git clone https://github.com/Geotab/sdk-dotnet-samples.git sdk-dotnet-samples
> cd sdk-dotnet-samples
> cd ImportZonesShapeFile
> dotnet run "my.geotab.com" "database" "user@email.com" "password" --namePrefix="SDK DOTNET SAMPLE" "owensboro.csv"
```

### Parameters

`dotnet run <server> <database> <username> <password> [--nameAttr=<attr>] [--namePrefix=<prefix>] [--type=<name>] [--threshold=<number>] <inputFile>`
Name | Description | Required
--- | --- | ---
server | The server name (Example: my.geotab.com) | true
database | The database name (Example: G560) | true
username | The MyGeotab user name | true
password | The MyGeotab password | true
[--nameAttr=`<attr>`] | Name of the shape's attribute to use as the zone name. (Default: first attribute containing the word 'name') | false
[--namePrefix=`<prefix>`] | The name prefix. (Example: prefix + Name) | false
[--type=`<name>`] | Specify zone type (Default: customer) | false
[--threshold=`<number>`] | Simplify zones by removing redundant points. Any point within approximately `<threshold>` meters of a line connecting its neighbors will be removed. | false
inputFile | File name of the Shape file containing the shapes to import, with the appropriate extension (.shp). The accompanying files (.dbf, .prj, .shx) need to be located in the project folder. Alternatively, you can use a CSV file. | true

## CSV layout

```csv
[Zone_name]
[polygon_point_x], [polygon_point_y]
[polygon_point_x], [polygon_point_y]
.
.
[polygon_point_x], [polygon_point_y]
[Zone_name]
[polygon_point_x], [polygon_point_y]
.
.
```
