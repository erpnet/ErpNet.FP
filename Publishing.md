# How to publish

Each individual project should be configured with proper 
[nuget package ID-s](https://docs.microsoft.com/en-us/dotnet/core/tools/csproj#nuget-metadata-properties).

Obtain the nuget API key for our organization. You can find the key in ERP.bg's internal secret server
under the name `nuget API key for ERP.NET`.

To publish an individual package, issue the following command *inside the project's directory*:

```shell
dotnet pack -c Release /p:PackageVersion=<version>
cd bin/Release
dotnet nuget push <project name and version>.nupkg  -k <nuget api key>  -s https://www.nuget.org/
```

## General notes

* Make sure assembly version matches package version
* Change `ReleaseNotes` tag in the `.csproj` file
* Create a tag for the version
