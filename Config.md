# Config File
The config file configures the way the ErpNet.FP print server works.
The config file is called "appsettings.json" and is located in the same directory as ErpNet.FP.Server.

# The JSON structure of the config
The config has the following root elements
* **"Logging"** - configures the logging level of server
* **"ErpNet.FP"** - configures the specific options of the print server
* **"Kestrel"** - configures the options of the integrated Kestrel web server

# Example appsettings.json

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ErpNet.FP": {
    "AutoDetect": false,
    "Printers": {
      "dt279013": {
        "Uri": "bg.dt.p.isl.com://COM15"
      },
      "dt517985": {
        "Uri": "bg.dt.c.isl.com://COM11"
      },
      "dt525860": {
        "Uri": "bg.dt.x.isl.com://COM21"
      },
      "dy448967": {
        "Uri": "bg.dy.isl.com://COM7"
      },
      "ed311662": {
        "Uri": "bg.ed.isl.com://COM20"
      },
      "prn1": {
        "Uri": "bg.dt.x.isl.com://COM21"
      },
      "prn2": {
        "Uri": "bg.dt.c.isl.com://COM11"
      },
      "zk126720": {
        "Uri": "bg.zk.zfp.com://COM22"
      },
      "zk970105": {
        "Uri": "bg.zk.v2.zfp.com://COM23"
      }
    }
  },
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://0.0.0.0:8001"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 20480,
      "MaxRequestHeaderCount": 50
    }
  }
}
```

# "Logging" Section
The loggingg section configures the logging level. The allowed logging levels are:
* **None** - Not used for writing log messages. Specifies that a logging category should not write any messages.
* **Critical** - Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
* **Error** - Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a failure in the current activity, not an application-wide failure.
* **Warning** - Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application execution to stop.
* **Information** - Logs that track the general flow of the application. These logs should have long-term value.
* **Debug** - Logs that are used for interactive investigation during development. These logs should primarily contain information useful for debugging and have no long-term value.
* **Trace** - Logs that contain the most detailed messages. These messages may contain sensitive application data. These messages are disabled by default and should never be enabled in a production environment.

# "ErpNet.FP" Section
This section can contain the following configuration options
* **AutoDetect** - specifies whether the print server should try to auto-detect the available printers at startup
* **Printers** - contains a list of configured printers. 

Usually, when AutoDetect=false, the Printers option contains the list of detected and allowed printers. 
When AutoDetect=true, the Pritners section can be used to override the options for the auto-detected printers.
E.g., the Printers section options have priority over the auto-detected printers and settings.

## Printers Sub-section
Each element in this section contains information about a single printer:
* **Uri** - contains the Uri of the configured printer.

# "Kestrel" Section
This section contains options, recognized by the integrated Kestrel web server. 
For information about all Kestrel options, read:
https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel

The most used options are:
* **EndPoints/Http/Url** - can be used to configure the port on which the server to listen (default is 8001).
* **Limits/MaxRequestBodySize** - configures the max request size. Can be used if there is a need to send very large (not recommended) fiscal notes to the printer.

## Configuring Https
If you need to configure Https access, you need to have a certificate file.

Then, configure the "Kestrel" section in the following way:
```
    "Kestrel": {
        "EndPoints": {
            "HttpsInlineCertFile": {
                "Url":"https://<site>:8001",
                "Certificate": {
                    "Path":"<path to .pfx file>",
                    "Password":"<certificate password>"
                }
            }
        },
        "Limits": {
          "MaxConcurrentConnections": 100,
          "MaxConcurrentUpgradedConnections": 100,
          "MaxRequestBodySize": 20480,
          "MaxRequestHeaderCount": 50
        }        
    }
```

This config removes the "http" endpoint and allows only https acccess.

Note:
```
* **<site>** should contain the domain name of the site for which the certificate is issued.
```
