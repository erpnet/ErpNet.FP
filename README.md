# ErpNet.FP

The ErpNet.FP library and server are intended to facilitate the printing to fiscal printers.

The library provides methods to detect, get status, print receipts, reports and other documents to fiscal printers.

The ErpNet.FP http server provides a REST API with JSON input/output, which prints documents transactionally and returns result status.

The http server receives input through the REST API and uses different transports to communicate with the fiscal printers. The transports include:
* COM serial connection
* Bluetooth
* Http
* TCP/IP
* etc.

# Printer Id

Each printer is identified by its printerId. Although the library consumer does not need to know the exact specifics of the algorithm for generating the printerId, they are provided here for completeness:

1. For locally connected printers, the printerId is the printer serial number.
For example: "dy448967"

2. If a local printer is connected through multiple transports (for example: both COM and BT), the id for each next transport connection is assigned a consecutive number.
For example: "dy448967_1"
Note: The algorithm runs always in the same order. Unless transports are changed, the id would remain constant. However, it is advised to always use the default, non-numeric id.

3. For network printers, the printerId is provided in the configuration file.
For example: "FP_Room1"

# Printer Uri

When a printer is detected, the http server saves something, called printer Uri. The Uri contains the connection information to connect to the printer. It contains details about printer driver, transport and path to the printer. It is similar to the connection string pattern. Example Uris:
- bg.dy.isl.com://COM5
- bg.dy.isl.com://COM3
- bg.dt.isl.x.com://COM5
- bg.tr.zk.http://fp5.mycompany.com
- etc.

The printer Uri is currently used only internally in the http server. Still, it is exposed as part of the device info. In the future, there might be methods to use the printers through their Uri.

# Http Server Setup

In the case of COM and Bluetooth, the http server should be installed on the same machine as the fiscal printer (or some kind of low-latency redirection should be provided). For other protocols, the network fiscal printer needs just to be accessible through its respective protocol.

When the http server starts, it auto-detects the local printers (COM/BT) and reads the addresses (Uris) of the remote printers from a config file. Upon request to return the available printers (URL: "/printers"), it returns all detected + configured printers.

Currently, the http server is available only for Windows:
- ErpNet.FP.Win

However, versions for most major platforms are planned:
- ErpNet.FP.Linux
- ErpNet.FP.Mac
- ErpNet.FP.Android
- ErpNet.FP.iOS

License
----
"Simplified BSD License" or "FreeBSD License", see LICENSE.txt