# Releasing a new version

## Prerequisites

* **Operating System:** Windows (required to build the MSI installer).
* **.NET SDK:** You need to have the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
* **Build Tools:** You need **MSBuild** to run the build script. This can be obtained via:
    * [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/) (Recommended for build servers/CLI).
    * *OR* A full installation of **Visual Studio 2022** or newer.
* **WiX Toolset:** No manual installation is required. The project uses **WiX v4**, which automatically downloads the necessary SDKs and tools via NuGet during the build process.

## Steps

1.  **Update Version:**
    Open `output.xml` in the root folder and update the version number inside the `<Version>` tag (e.g., `1.1.0.1682`).

2.  **Git Tag:**
    Create a git tag with the same version number as in `output.xml` (prepend it with `v`, e.g., `v1.1.0.1682`).

3.  **Build:**
    Open a command prompt where `msbuild` is available (e.g., "Developer PowerShell"), navigate to the project root, and run:
    ```cmd
    msbuild output.xml /t:All
    ```
    *(Note: This script automatically handles Restore, Publish for all platforms, Cache Busting, and MSI generation).*

4.  **Verify Artifacts:**
    Check the `./Output` folder. It should contain:
    * **ZIP archives** for cross-platform releases (`win-x86.zip`, `linux-x64.zip`, `osx-x64.zip`, etc.).
    * **MSI Installers** located in language-specific subfolders:
        * `./Output/en-US/ErpNet.FP.Setup.msi` (English)
        * `./Output/bg-BG/ErpNet.FP.Setup.msi` (Bulgarian)

5.  **Commit & Push:**
    Inspect the changes (version bumps in `Product.wxs` and `Directory.Build.props` will happen automatically). Commit these changes and push to the server (including the tag).

6.  **Release:**
    Open the [releases page](https://github.com/erpnet/ErpNet.FP/releases/new), create a new release using the tag from step 2, and upload the files from the `Output` folder (ZIPs and MSIs).

---

## Details about the build/release process

### Increment `Version` in `output.xml`

Open [output.xml](output.xml) and update `<Version>` under `/Project/PropertyGroup`.

When you run the build, this version is automatically propagated to:
* The WiX v4 Package (`ErpNet.FP.Setup\Product.wxs`).
* The .NET Projects (`Directory.Build.props` or project files).

### Details about `output.xml`

The `output.xml` is an MSBuild orchestration script that performs the following:

1.  **Cache Busting:**
    Updates `index.html` by appending `?ver=SHA256_HASH` to the `app.js` and `index.css` references. This ensures clients automatically retrieve new versions of the frontend code instead of using cached browser files.
2.  **Multi-Platform Publishing:**
    Runs `dotnet publish` for .NET 8 targeting specific Runtime Identifiers (`win-x86`, `win-x64`, `linux-x64`, `linux-arm`, `osx-x64`).
3.  **Zipping:**
    Compresses the published binaries into `.zip` files in the `./Output` folder.
4.  **Windows Installer (WiX v4):**
    Builds the `ErpNet.FP.Setup` project targeting **x86**.
    * *Note:* WiX v4 generates localized installers in subfolders (`Output/en-US/` and `Output/bg-BG/`).

### Building On Windows (Full Release)

Generates **ZIP archives** for all platforms and the **Windows MSI installer**.

```
msbuild output.xml /t:All
```

### Building on other platforms (ZIP only)

Generates only the **ZIP archives** (skips the Windows MSI installer). Use this command on Linux, macOS, or if you do not have WiX installed.

```
dotnet msbuild output.xml -t:PrepareForRelease -t:PublishToOutputPath
```

## Troubleshooting

If you encounter errors regarding **"Target assets not found"** or **"NETSDK1047"**:

1. Run `dotnet clean` in the root directory.
2. Manually delete `obj` and `bin` folders in `ErpNet.FP.Server` and `ErpNet.FP.Core`.
3. Run the build command again.
