On upgrading to new versions

* Download the latest Tremol SDK
* Files FP.cs is part of the SDK, in Libs/C#. Copy-paste here.
* FPCore.cs is reverse-engineered from FPCore.dll in Libs/C#. Use ILSpy to open the DLL, then copy-paste the 
contents of the file in FPCore.cs. Doing this is required, because FPCore.dll is compiled in a way
that a netcoreapp process, even when compiled as 32-bit, crashes when the DLL gets linked to the executable.
There is no valid reason for this file to be 32-bit only. All it does is serialization of requests to/from
the ZfpLabs server via HTTP GET and POST requests with XML payload.