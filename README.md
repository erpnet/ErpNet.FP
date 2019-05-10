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

### For the impatient, here are some examples

- [Postman collection] of examples

### You want to try it now? You can download windows version of ErpNet.FP

-  You can downloading ErpNet.FP.Win Http Server, and its manager ErpNet.FP.Win.Manager.exe here -> [ErpNet.FP.Win.zip].

Inside the zip file, you will find two Windows executables: ErpNet.FP.Win.exe (service) and ErpNet.FP.Win.Manager.exe (service manager). 

Run ErpNet.FP.Win.Manager.exe and it will start the ErpNet.FP.Win.exe automatically from the same directory, and will be waiting for your commands as notification icon in the tray.

When you exit the session, you can examine debug.log file for every event occured while using the service. 

Also, you can use "Show console", and "Exit" options from the service manager tray icon.

## Some concepts needs to be explained

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
```json
{
  "dt525860": {
    "uri": "bg.dt.x.isl.com://COM21",
    "serialNumber": "DT525860",
    "fiscalMemorySerialNumber": "02525860",
    "company": "Datecs",
    "model": "FP-700X",
    "firmwareVersion": "266207 29Jan19 1634",
    "itemTextMaxLength": 22,
    "commentTextMaxLength": 42,
    "operatorPasswordMaxLength": 8
  },
  "dt517985": {
    "uri": "bg.dt.c.isl.com://COM11",
    "serialNumber": "DT517985",
    "fiscalMemorySerialNumber": "02517985",
    "company": "Datecs",
    "model": "DP-25",
    "firmwareVersion": "263453 08Nov18 1312",
    "itemTextMaxLength": 22,
    "commentTextMaxLength": 42,
    "operatorPasswordMaxLength": 8
  },
  "zk126720": {
    "uri": "bg.zk.zfp.com://COM22",
    "serialNumber": "ZK126720",
    "fiscalMemorySerialNumber": "50163145",
    "company": "Tremol",
    "model": "M20",
    "firmwareVersion": "Ver. 1.01 TRA20 C.S. 2541",
    "itemTextMaxLength": 32,
    "commentTextMaxLength": 30,
    "operatorPasswordMaxLength": 6
  },
  "zk970105": {
    "uri": "bg.zk.v2.zfp.com://COM23",
    "serialNumber": "ZK970105",
    "fiscalMemorySerialNumber": "50970105",
    "company": "Tremol",
    "model": "FP01-KL-V2",
    "firmwareVersion": "99C4",
    "itemTextMaxLength": 32,
    "commentTextMaxLength": 30,
    "operatorPasswordMaxLength": 6
  },
  "dt279013": {
    "uri": "bg.dt.p.isl.com://COM15",
    "serialNumber": "DT279013",
    "fiscalMemorySerialNumber": "02279013",
    "company": "Datecs",
    "model": "FP-2000",
    "firmwareVersion": "1.00BG 23NOV18 1000",
    "itemTextMaxLength": 22,
    "commentTextMaxLength": 42,
    "operatorPasswordMaxLength": 8
  },
  "ed311662": {
    "uri": "bg.ed.isl.com://COM20",
    "serialNumber": "ED311662",
    "fiscalMemorySerialNumber": "44311662",
    "company": "Eltrade",
    "model": "A1",
    "firmwareVersion": "KL5101.1811.0.3 15Nov18 15:49",
    "itemTextMaxLength": 30,
    "commentTextMaxLength": 46,
    "operatorPasswordMaxLength": 8
  },
  "dy448967": {
    "uri": "bg.dy.isl.com://COM7",
    "serialNumber": "DY448967",
    "fiscalMemorySerialNumber": "36607003",
    "company": "Daisy",
    "model": "CompactM",
    "firmwareVersion": "ONL-4.01BG",
    "itemTextMaxLength": 20,
    "commentTextMaxLength": 28,
    "operatorPasswordMaxLength": 6
  }
}
```

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
