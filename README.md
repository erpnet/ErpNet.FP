<img src="https://github.com/erpnet/ErpNet.FP/raw/master/ErpNet.FP.Server/ErpNet.FP.thumb.png" align="right"/>

# ErpNet.FP

ErpNet.FP is a light-weight multi-platform Http server facilitating printing to fiscal printers through simple JSON Api. The library provides methods to detect, get status, print receipts, reports and other documents to fiscal printers.

The ErpNet.FP http server provides a REST API with JSON input/output, which prints documents transactionally and returns result status.

The http server receives input through the REST API and uses different transports to communicate with the fiscal printers. The transports include:
* COM serial connection
* Bluetooth
* TCP/IP
* etc.

# The JSON Protocol
## Requests
The print server accepts documents for printing, using JSON based protocol. 
For example, this would print the specified receipt to dt517985, which is the printerId of one of the detected printers, listed with GET /printers (see below for printerId explanation):

POST /printers/dt517985/receipt

```json
{
  "uniqueSaleNumber": "DT279013-DD01-0000001",
  "items": [
    {
      "text": "Cheese",
      "quantity": 1,
      "unitPrice": 12,
      "taxGroup": 2
    },
    {
      "type": "comment",
      "text": "Additional comment to the cheese..."
    },
    {
      "text": "Milk",
      "quantity": 2,
      "unitPrice": 10,
      "taxGroup": 2,
      "priceModifierValue": 10,
      "priceModifierType": "discount-percent"
    }
  ],
  "payments": [
    {
      "amount": 30,
      "paymentType": "cash"
    }
  ]
}
```
## For More Information
For more information, see the [full documentation of the protocol](https://github.com/erpnet/ErpNet.FP/blob/master/PROTOCOL.md).

## Response - Interpreting The Results
The most important result is the "ok" field. It contains "true" when the POST operation was successful, otherwise - "false".
If there was error, "ok" would be "false".

If "ok"="false", it is guaranteed, that at least one message of type "error" would be present.

The error and warning messages have standardized codes across all manufacturers. The standard error and warning codes are listed in the 
[Error and Warning Codes](ErrorCodes.md) file.

The standard error codes are a subset of all manufacturer codes and flags. 
In some cases, the specific manufacturer codes, flags and messages could contain more detailed information. 
The manufacturer code, when available, is contained in the "originalCode" field. 
The problem with using the manufacturer codes is that they are different for each manufacturer. 
For some manufacturers they are not even present (there might be just some status flags). 
The manufacturer codes can even change between revisions of printers of the same manufacturer. 
The standardized error and warning codes are guaranteed to be the same across all manufacturers and printer versions.
Messages with "type": "info", have no codes, because they cannot be standardized.

### Example Return JSON (No Problems) after printing receipt:
```json
{
  "ok": "true",
  "messages": [
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ],
  "receiptNumber": "0000085",
  "receiptDateTime": "2019-05-17T13:55:18",
  "receiptAmount": 30,
  "fiscalMemorySerialNumber": "02517985"
}
```

### Example Return JSON (Warning) while getting the status:
```json
{
  "ok": "true",
  "messages": [
    {
      "type": "warning",
      "code": "W201",
      "text": "The fiscal memory almost full"
    },
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ],
  "deviceDateTime": "2019-05-10T15:50:00"
}
```

### Example Return JSON (Error):
```json
{
  "ok": "false",
  "messages": [
    {
      "type": "error",
      "code": "E201",
      "text": "The fiscal memory is full"
    },
    {
      "type": "info",
      "text": "Serial number and number of FM are set"
    },
    {
      "type": "info",
      "text": "FM is formatted"
    }
  ]
}
```

# Eager to try? 
You can list and [download the binaries for ErpNet.FP.Server]:

* Windows 32/64 bit - [ErpNet.FP.Server.zip - Windows 32 bit], [ErpNet.FP.Server.zip - Windows 64 bit] -  
Download and unzip the file in a folder. Inside the folder you will find executable file: ErpNet.FP.Server.exe.
To start the printing service, run ErpNet.FP.Server.exe. 
In the default configuration, you can browse the Admin page at http://localhost:8001.

* OSX 10.10 and up, 64 bit - [ErpNet.FP.Server.zip - macOS] - You can download and unzip the server in a folder. 
Inside the unzipped folder, run it from console/terminal with:
```bash
./ErpNet.FP.Server
```

* Linux 64 bit - [ErpNet.FP.Server.zip - Linux x64] - You can download and unzip the server. 
Because of the serial ports default permissions, run it from sudoer, with sudo, or in the root user context. 
For convenience, create a systemd service config file, get more info how to do it here [Systemd: Service File Examples].
The other way is to run it from non-root user, but that user should have permissions to read and write to serial ports.

* Linux-Arm 64 bit - [ErpNet.FP.Server.zip - Linux Arm] - You can download and unzip the server. 
This version is compatible with Raspberry PI OS, Raspbian, and 64 bit Arm devices like Raspberry PI 3B+, Raspberry PI 2 and up.
Because of the serial ports default permissions, run it from sudoer, with sudo, or in the root user context. 
For convenience, create a systemd service config file, get more info how to do it here [Systemd - Raspberry Pi Documentation].
The other way is to run it from non-root user, but that user should have permissions to read and write to serial ports.

## Debug information 

After you stop the service ErpNet.FP.Server, you can examine the debug.log file. It contains debug information for every event that occured while using the service. 

## Server Configuration
The server configuration options are located in a file, called "appsettings.json". For more information, see [Configuration](https://github.com/erpnet/ErpNet.FP/blob/master/Config.md).

## Fiscal Printer Setup
Most fiscal printers or cash register should be in "Fiscal Printer" mode in order to accept commands from the PC through the serial interface. Refer to your printer manual how to setup the fiscal printer mode.

The following non-comprehensive article contains the setup procedure for some fiscal printers:
* [Popular Bulgarian Fiscal Printers Setup](BGFiscalPrinterSetup.md)

# Concepts

## Printer Id

Each printer is identified by its printerId. Although the library consumer does not need to know the exact specifics of the algorithm for generating the printerId, they are provided here for completeness:

1. For locally connected printers, the printerId is the printer serial number.
For example: "dy448967"

2. If a local printer is connected through multiple transports (for example: both COM and BT), the id for each next transport connection is assigned a consecutive number.
For example: "dy448967_1"
Note: The algorithm runs always in the same order. Unless transports are changed, the id would remain constant. However, it is advised to always use the default, non-numeric id.

3. For network printers, the printerId is provided in the configuration file.
For example: "FP_Room1"

## Printer Uri

When a printer is detected, the http server saves something, called printer Uri. The Uri contains the connection information to connect to the printer. It contains details about printer driver, transport and path to the printer. It is similar to the connection string pattern. Example Uris:
- bg.dy.isl.com://COM5
- bg.dy.isl.com://COM3
- bg.dt.x.isl.com://COM21
- bg.zk.zfp.http://fp5.mycompany.com
- bg.dt.p.isl.tcp://192.168.1.77:9100
- etc.

The printer Uri is currently used only internally in the http server. Still, it is exposed as part of the device info. In the future, there might be methods to use the printers through their Uri.

# Future plans

Currently, the http server is available for:
- Windows 32/64 bit
- macOS 10.10 and up
- Linux x64
- Linux Arm

However, versions for mobile platforms are planned:
- Android
- iOS

# Currently supported
The library supports printers from the following manufacturers:
* Datecs (http://www.datecs.bg)
* Tremol (https://www.tremol.bg)
* Daisy (https://daisy.bg/)
* Eltrade (https://www.eltrade.com)
* Incotex (http://www.incotex.bg/) 

If you want your device to be supported, please contact us, and we will try our best to help you!

## Tested on
- Datecs DP-25, firmware: 263453 08Nov18 131, protocol: bg.dt.c.isl.com
- Datecs WP-50, firmware: 261403 08Nov18 1050, protocol: bg.dt.c.isl.com
- Datecs FP-2000, firmware: 1.00BG 23NOV18 1000, protocols: bg.dt.p.isl.com, bg.dt.p.isl.tcp
- Datecs FP-700X, firmware: 266207 29Jan19 1634, protocols: bg.dt.x.isl.com, bg.dt.x.isl.tcp
- Daisy CompactM, firmware: ONL-4.01BG, protocol: bg.dy.isl.com
- Daisy CompactM, firmware: ONL01-4.01BG, protocol: bg.dy.isl.com
- Eltrade A1, firmware: KL5101.1811.0.3 15Nov18 15:49, protocol: bg.ed.isl.com
- Tremol FP01-KL-V2, firmware: 99C4, protocol: bg.zk.v2.zfp.com
- Tremol M20, firmware: Ver. 1.01 TRA20 C.S. 25411, protocol: bg.zk.zfp.com
- Incotex 300SM KL-Q, firmware: 2.11 Jan 22 2019 14:00, protocol: bg.in.isl.com

## Supported protocols and devices
* bg.dt.c.isl - Datecs WP-50, Datecs DP-05, Datecs DP-05B, Datecs DP-05C, Datecs DP-25, Datecs DP-35, Datecs DP-150, Datecs DP-15
* bg.dt.p.isl - Datecs FP-650, Datecs FP-800, Datecs FP-2000, Datecs FMP-10, Datecs SK1-21F, Datecs SK1-31F
* bg.dt.x.isl - Datecs DP-25X, Datecs FMP-350X, Datecs FP-700X, Datecs WP-500X, Datecs FMP-55X, Datecs WP-50X, Datecs FP-700X
* bg.zk.zfp - Tremol A19Plus, Tremol S21, Tremol M23, Tremol M20, Tremol FP15
* bg.zk.v2.zfp - Tremol Z-KL-V2, Tremol ZM-KL-V2, Tremol ZS-KL-V2, Tremol FP01-KL V2, Tremol FP05-KL V2, Tremol M-KL-V2, Tremol S-KL-V2, Tremol FP15 KL V2, Tremol FP03-KL V2, Tremol FP07-KL V2
* bg.ed.isl - Eltrade A1 KL, Eltrade A1 KL, Eltrade A3 KL, Eltrade B1 KL, Eltrade PRP 250F KL, Eltrade A6 KL, Eltrade B3 KL 
* bg.dy.isl - Daisy Compact S, Daisy Compact M
* bg.in.isl - Incotex 133 KL-Q, Incotex 181 KL-Q, Incotex 777, Incotex 300SM KL-Q, INCOTEX 300S KL-Q


* **Didn't find your device on the list?** Please, create an issue here in the project and we will check whether we can support it with the current set of protocols or we need to implement a new one.

# Source Code
To compile and run the source code, you will need .Net Core 3.0 Preview 6 SDK.
As IDE for Windows, you can use Visual Studio 2019, version 16.2 Preview 2 or newer. 
For macOS, you can use Visual Studio for Mac 8.2 Preview.

# License
"Simplified BSD License" or "FreeBSD License", see [LICENSE.txt]

# Contributing
See our [Contributing] document and our [Code of Conduct] document, to learn how to help us.

[Postman collection]: <https://documenter.getpostman.com/view/6751288/S1a358Uz>
[LICENSE.txt]: <LICENSE.txt>
[Contributing]: <CONTRIBUTING.md>
[Code of Conduct]: <CODE_OF_CONDUCT.md>
[Systemd: Service File Examples]: https://www.shellhacks.com/systemd-service-file-example/
[Systemd - Raspberry Pi Documentation]: https://www.raspberrypi.org/documentation/linux/usage/systemd.md
[ErpNet.FP.Server.zip - Windows 32 bit]: <https://github.com/erpnet/ErpNet.FP/releases/download/v.1.0.703.2100/win-x86.zip>
[ErpNet.FP.Server.zip - Windows 64 bit]: <https://github.com/erpnet/ErpNet.FP/releases/download/v.1.0.703.2100/win-x64.zip>
[ErpNet.FP.Server.zip - macOS]: <https://github.com/erpnet/ErpNet.FP/releases/download/v.1.0.703.2100/osx-x64.zip>
[ErpNet.FP.Server.zip - Linux x64]: <https://github.com/erpnet/ErpNet.FP/releases/download/v.1.0.703.2100/linux-x64.zip>
[ErpNet.FP.Server.zip - Linux Arm]: <https://github.com/erpnet/ErpNet.FP/releases/download/v.1.0.703.2100/linux-arm.zip>
[download the binaries for ErpNet.FP.Server]: <https://github.com/erpnet/ErpNet.FP/releases/tag/v.1.0.703.2100>
