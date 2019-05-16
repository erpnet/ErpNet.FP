<img src="https://github.com/erpnet/ErpNet.FP/raw/master/ErpNet.FP.Win/ErpNet.FP.Win.ico" align="right"/>

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

### The JSON protocol
The JSON protocol is documented in a series of postman examples:
- [Postman collection] of examples

### Want to try it now? Download the windows version of ErpNet.FP

-  You can download the ErpNet.FP.Win Http Server and its manager app ErpNet.FP.Win.Manager here -> [ErpNet.FP.Win.zip].

To start the printing service, run ErpNet.FP.Win.Manager.exe. It start and show notification icon in the tray.

Starting the manager also starts the service. Stopping the manager stops the service.

### Console and debug information
The tray icon contains a menu option - "Show console". It can be used to show console, showing real-time information about the inner workings of the printing service.

After you stop the manager, you can examine the debug.log file. It contains debug information for every event that occured while using the service. 

## Concepts

### Printer Id

Each printer is identified by its printerId. Although the library consumer does not need to know the exact specifics of the algorithm for generating the printerId, they are provided here for completeness:

1. For locally connected printers, the printerId is the printer serial number.
For example: "dy448967"

2. If a local printer is connected through multiple transports (for example: both COM and BT), the id for each next transport connection is assigned a consecutive number.
For example: "dy448967_1"
Note: The algorithm runs always in the same order. Unless transports are changed, the id would remain constant. However, it is advised to always use the default, non-numeric id.

3. For network printers, the printerId is provided in the configuration file.
For example: "FP_Room1"

### Printer Uri

When a printer is detected, the http server saves something, called printer Uri. The Uri contains the connection information to connect to the printer. It contains details about printer driver, transport and path to the printer. It is similar to the connection string pattern. Example Uris:
- bg.dy.isl.com://COM5
- bg.dy.isl.com://COM3
- bg.dt.isl.x.com://COM5
- bg.tr.zk.http://fp5.mycompany.com
- etc.

The printer Uri is currently used only internally in the http server. Still, it is exposed as part of the device info. In the future, there might be methods to use the printers through their Uri.

### Future plans

Currently, the http server is available only for Windows:
- ErpNet.FP.Win

However, versions for most major platforms are planned:
- ErpNet.FP.Linux
- ErpNet.FP.Mac
- ErpNet.FP.Android
- ErpNet.FP.iOS

### Currently supported manifacturers
Datecs (http://www.datecs.bg),
Tremol (https://www.tremol.bg),
Daisy (https://daisy.bg/),
Eltrade (https://www.eltrade.com).

If you want your device to be supported, please contact us, and we will try our best to help you!

### Tested on
- Datecs DP-25, firmware: 263453 08Nov18 131, protocol: bg.dt.c.isl.com
- Datecs FP-2000, firmware: 1.00BG 23NOV18 1000, protocol: bg.dt.p.isl.com
- Datecs FP-700X, firmware: 266207 29Jan19 1634, protocol: bg.dt.x.isl.com
- Daisy CompactM, firmware: ONL-4.01BG, protocol: bg.dy.isl.com
- Daisy CompactM, firmware: ONL01-4.01BG, protocol: bg.dy.isl.com
- Eltrade A1, firmware: KL5101.1811.0.3 15Nov18 15:49, protocol: bg.ed.isl.com
- Tremol FP01-KL-V2, firmware: 99C4, protocol: bg.zk.v2.zfp.com
- Tremol M20, firmware: Ver. 1.01 TRA20 C.S. 25411, protocol: bg.zk.zfp.com

License
----
"Simplified BSD License" or "FreeBSD License", see [LICENSE.txt]

Contributing
----
See our [Contributing] document and our [Code of Conduct] document, to learn how to help us.

[Postman collection]: <https://documenter.getpostman.com/view/6751288/S1EJYMg5>
[LICENSE.txt]: <https://raw.githubusercontent.com/erpnet/ErpNet.FP/master/LICENSE.txt>
[ErpNet.FP.Win.zip]: <https://github.com/erpnet/ErpNet.FP/raw/master/ErpNet.FP.Win/Published/ErpNet.FP.Win.zip>
[Contributing]: <https://github.com/erpnet/ErpNet.FP/blob/master/CONTRIBUTING.md>
[Code of Conduct]: <https://github.com/erpnet/ErpNet.FP/blob/master/CODE_OF_CONDUCT.md>
