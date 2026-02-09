# Releasing a new version

## Prerequisites

* You need to have [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/current) installed for your operating system
* You need to have a newer version of MSBuild (>=15.8) in your `PATH`.
* If you are on a Windows machine:
	* Make sure you have [WIX](https://wixtoolset.org/releases/) v4 installed (for setup)


## Steps

1. Update `Version` in `output.xml`
2. Create a git tag with the same version as in `output.xml`, and prepend it with v
3. Build on Windows via `msbuild output.xml`
4. Inspect the changes, and push the changes to the server (with the new tag)
5. Open the [releases page](https://github.com/erpnet/ErpNet.FP/releases/new) and create a new
release with the tag you created in step 4
6. Upload the version in 

## Details about the build/release process

### Increment `Version` in `output.xml`

Open up [output.xml](output.xml), and update `Version` under `/Project/PropertyGroup` to a newer version.

Then, depending on your operating system, you will have to build the project.

### Details about `output.xml`

Building the project 
	* updates Version constants for .NET assembies, makes sure that
	[index.css](ErpNet.FP.Server\wwwroot\index.css) and 
	[app.js](ErpNet.FP.Server\wwwroot\app.js) have `?ver=SHA256_HEX` appended, so that whenever you make changes
	to those files, clients will automatically retrieve the new versions and not have older, cached files stored
	in their browsers;
	* the windows setup's version is updated
	* .NET projects are published and zipped for various architectures in `./Output`
	* Windows setup is built and placed in './Output'

### Building On Windows

```
msbuild /Restore /t:All output.xml
```

### Building on other platforms

```
msbuild /t:PrepareForRelease /t:PublishToOutputPath output.xml
```