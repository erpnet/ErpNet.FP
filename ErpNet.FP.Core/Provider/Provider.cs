#nullable enable
namespace ErpNet.FP.Core.Provider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using ErpNet.FP.Core.Configuration;
    using ErpNet.FP.Core.Drivers;
    using ErpNet.FP.Core.Transports;
    using Serilog;
    using static ErpNet.FP.Core.Transports.ComTransport;

    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public class Provider
    {
        public Provider(ServiceOptions serviceOptions)
        {
            this.ServiceOptions = serviceOptions;
        }
        /// <summary>
        /// Adds the specified protocol to the provider.
        /// A protocol consists of a printer driver and a transport.
        /// </summary>
        /// <param name="driver">The driver.</param>
        /// <param name="transport">The transport.</param>
        public Provider Register(FiscalPrinterDriver driver, Transport transport)
        {
            var key = driver.DriverName + "." + transport.TransportName;
            protocols.Add(key, (driver, transport));
            return this;
        }

        public async Task<IFiscalPrinter?> DetectPrinterAsync(
            IChannel channel,
            Transport transport,
            List<FiscalPrinterDriver> drivers)
        {
            IFiscalPrinter? printer = null;
            var unknownDeviceConnectedToChannel = true;
            var positionInList = 1;
            var detectionTask = Task.Run(async () =>
            {
                foreach (var driver in drivers)
                {
                    try
                    {
                        var uri = $"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}";
                        Log.Information($"Probing ({positionInList++}/{drivers.Count}){uri}...");

                        printer = await Task<IFiscalPrinter>.Run(() => driver.Connect(channel, ServiceOptions));
                        printer.DeviceInfo.Uri = uri;

                        // We found our driver, so do not test more
                        unknownDeviceConnectedToChannel = false;
                        Log.Information($"Successfully detected {uri}.");
                        break;
                    }
                    catch (TimeoutException ex)
                    {
                        // Timeout occured while connecting. Skip this transport address.
                        Log.Error($"Timeout occured: {ex.Message}");
                    }
                    catch (InvalidResponseException ex)
                    {
                        // If speed not matched, then skip this transport address detection.
                        if (ex.Message.Contains("communication speed not match"))
                            break;
                        // Autodetect probe not passed for this channel. No response.
                        Log.Information($"Device at {channel.Descriptor} incompatible with protocol {driver.DriverName}: {ex.Message}");
                    }
                    catch (InvalidDeviceInfoException ex)
                    {
                        // Autodetect probe not passed for this channel. Invalid device.
                        Log.Information($"Device at {channel.Descriptor} incompatible with protocol {driver.DriverName}: invalid device info returned ({ex.Message}).");
                    }
                    catch (FileNotFoundException)
                    {
                        // Port not exists
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unexpected error: {ex.Message}");
                    }
                }
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(ServiceOptions.DetectionTimeout));

            var completedTask = await Task.WhenAny(detectionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Log.Error($"Detection for channel {channel.Descriptor} timed out after {ServiceOptions.DetectionTimeout} seconds.");
                return null;
            }

            if (unknownDeviceConnectedToChannel)
            {
                // We did not recognize the device, so drop that channel 
                // and leave it available for others
                // transport.Drop(channel);   // hangs here if device driver problem occure
            }
            Log.Information($"Probing finished for {channel.Descriptor}.");
            return printer;
        }


        /// <summary>
        /// Returns the available fiscal printers.
        /// </summary>
        /// <returns>The available fiscal printers.</returns>
        public IDictionary<string, IFiscalPrinter> DetectAvailablePrinters(IEnumerable<string> excludedPorts)
        {
            var fp = new Dictionary<string, IFiscalPrinter>();
            var transportDrivers = new Dictionary<Transport, List<FiscalPrinterDriver>>();

            foreach (var (driver, transport) in protocols.Values)
            {
                if (!transportDrivers.ContainsKey(transport))
                {
                    transportDrivers[transport] = new List<FiscalPrinterDriver>();
                }
                transportDrivers[transport].Add(driver);
            }

            List<Task<IFiscalPrinter?>> listOfTasks = new List<Task<IFiscalPrinter?>>();
            foreach (KeyValuePair<Transport, List<FiscalPrinterDriver>> td in transportDrivers)
            {
                var transport = td.Key;
                var drivers = td.Value;
                foreach (var (address, _) in transport.GetAvailableAddresses())
                {
                    if ( excludedPorts.ToList().Contains(address))
                    {
                        Log.Information($"Port {address} is excluded from probing.");
                        continue;
                    }
                    try
                    {
                        var channel = transport.OpenChannel(address);
                        listOfTasks.Add(DetectPrinterAsync(channel, transport, drivers));
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Cannot open channel {address}: {ex.Message}");
                    }
                }
            }

            try
            {
                var task = (Task.WhenAll<IFiscalPrinter?>(listOfTasks));
                task.Wait();
                foreach (var printer in task.Result)
                {
                    if (printer != null)
                    {
                        fp.Add(printer.DeviceInfo.Uri, printer);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Probing error: {ex.Message}");
            }
            return fp;
        }

        private static readonly Regex uriPattern = new Regex(
            @"^(?<protocol>.+)://(?<address>.+)$");

        private static readonly string[] TremolZfpTcpProtocols =
        {
            "bg.zk.zfp.tcp",
            "bg.zk.v2.zfp.tcp"
        };

        private const string DeviceUriSecretMask = "<password>";
        private const string DeviceUriTokenTrailingCharacters = ".,;)]}\"'";

        private static readonly Regex deviceUriInTextPattern = new Regex(
            @"[a-zA-Z][a-zA-Z0-9+.\-]*://[^\s""'<>]+",
            RegexOptions.IgnoreCase);

        public static bool IsMaskedDeviceUri(string? deviceUri)
        {
            return TrySplitDeviceUriSecret(deviceUri, out _, out var secret, out var secretSupplied)
                && secretSupplied
                && string.Equals(secret, DeviceUriSecretMask, StringComparison.Ordinal);
        }

        public static bool HasDeviceUriSecret(string? deviceUri)
        {
            return TrySplitDeviceUriSecret(deviceUri, out _, out _, out var secretSupplied)
                && secretSupplied;
        }

        public static string? RedactDeviceUri(string? deviceUri)
        {
            if (deviceUri == null)
            {
                return null;
            }

            if (!TrySplitDeviceUriSecret(deviceUri, out var uriWithoutSecret, out _, out var secretSupplied)
                || !secretSupplied)
            {
                return deviceUri;
            }

            return $"{uriWithoutSecret}:{DeviceUriSecretMask}";
        }


        public static bool MightContainDeviceUri(string? text)
        {
            return text != null && text.IndexOf("://", StringComparison.Ordinal) >= 0;
        }

        public static bool MightContainDeviceUriSecret(string? text)
        {
            if (text == null)
            {
                return false;
            }

            var schemeSeparator = text.IndexOf("://", StringComparison.Ordinal);
            if (schemeSeparator < 0 || !IsTcpDeviceScheme(text.Substring(0, schemeSeparator)))
            {
                return false;
            }

            var address = text.Substring(schemeSeparator + 3);
            var hostEnd = address.IndexOf(':');
            return hostEnd >= 0 && address.IndexOf(':', hostEnd + 1) >= 0;
        }

        public static string RedactDeviceUrisInText(string? text)
        {
            if (!MightContainDeviceUri(text))
            {
                return text ?? string.Empty;
            }

            return deviceUriInTextPattern.Replace(text!, match =>
            {
                var token = match.Value;
                var coreEnd = token.Length;
                while (coreEnd > 0
                    && DeviceUriTokenTrailingCharacters.IndexOf(token[coreEnd - 1]) >= 0)
                {
                    coreEnd--;
                }

                var trailing = token.Substring(coreEnd);
                var core = token.Substring(0, coreEnd);
                string redacted;
                if (HasDeviceUriSecret(core))
                {
                    redacted = RedactDeviceUri(core) ?? DeviceUriSecretMask;
                }
                else if (MightContainDeviceUriSecret(core) && !core.EndsWith(":", StringComparison.Ordinal))
                {
                    redacted = DeviceUriSecretMask;
                }
                else
                {
                    redacted = core;
                }
                return redacted + trailing;
            });
        }

        internal static bool HasSameDeviceConnection(string firstDeviceUri, string secondDeviceUri)
        {
            return TrySplitDeviceUriSecret(
                    firstDeviceUri,
                    out var firstUriWithoutSecret,
                    out var firstSecret,
                    out var firstSecretSupplied)
                && TrySplitDeviceUriSecret(
                    secondDeviceUri,
                    out var secondUriWithoutSecret,
                    out var secondSecret,
                    out var secondSecretSupplied)
                && string.Equals(firstUriWithoutSecret, secondUriWithoutSecret, StringComparison.OrdinalIgnoreCase)
                && firstSecretSupplied == secondSecretSupplied
                && string.Equals(firstSecret, secondSecret, StringComparison.Ordinal);
        }

        public static bool TryRestoreDeviceUriSecret(
            string existingDeviceUri,
            string requestedDeviceUri,
            out string deviceUriToStore)
        {
            deviceUriToStore = requestedDeviceUri;
            if (!TrySplitDeviceUriSecret(
                    requestedDeviceUri,
                    out var requestedUriWithoutSecret,
                    out var requestedSecret,
                    out var requestedSecretSupplied))
            {
                return true;
            }

            if (!requestedSecretSupplied)
            {
                return true;
            }

            if (!string.Equals(requestedSecret, DeviceUriSecretMask, StringComparison.Ordinal))
            {
                return true;
            }

            if (!TrySplitDeviceUriSecret(
                    existingDeviceUri,
                    out _,
                    out var existingSecret,
                    out var existingSecretSupplied)
                || !existingSecretSupplied)
            {
                return false;
            }

            deviceUriToStore = $"{requestedUriWithoutSecret}:{existingSecret}";
            return true;
        }


        private static bool IsTcpDeviceScheme(string? scheme)
        {
            return scheme != null && scheme.EndsWith(".tcp", StringComparison.OrdinalIgnoreCase);
        }
        private static bool TryGetTremolZfpTcpUriPrefix(
            string deviceUri,
            out string protocol,
            out string prefix)
        {
            protocol = string.Empty;
            prefix = string.Empty;
            foreach (var candidate in TremolZfpTcpProtocols)
            {
                if (deviceUri.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    protocol = candidate;
                    prefix = candidate + "://";
                    return true;
                }
            }
            return false;
        }

        private static bool TrySplitDeviceUriSecret(
            string? deviceUri,
            out string uriWithoutSecret,
            out string? secret,
            out bool secretSupplied)
        {
            uriWithoutSecret = string.Empty;
            secret = null;
            secretSupplied = false;

            if (deviceUri == null)
            {
                return false;
            }

            var schemeSeparator = deviceUri.IndexOf("://", StringComparison.Ordinal);
            if (schemeSeparator < 0 || !IsTcpDeviceScheme(deviceUri.Substring(0, schemeSeparator)))
            {
                return false;
            }

            var address = deviceUri.Substring(schemeSeparator + 3);
            var hostEnd = address.IndexOf(':');
            if (hostEnd <= 0)
            {
                return false;
            }

            var secretSeparator = address.IndexOf(':', hostEnd + 1);
            var portLength = secretSeparator < 0
                ? address.Length - hostEnd - 1
                : secretSeparator - hostEnd - 1;
            var portText = address.Substring(hostEnd + 1, portLength);
            if (portText.Length == 0 || !int.TryParse(portText, out _))
            {
                return false;
            }

            uriWithoutSecret = deviceUri.Substring(0, schemeSeparator + 3 + hostEnd + 1 + portLength);
            if (secretSeparator < 0)
            {
                return true;
            }

            var secretText = address.Substring(secretSeparator + 1);
            if (secretText.Length > 0)
            {
                secret = secretText;
                secretSupplied = true;
            }
            return true;
        }
        private static bool TrySplitTremolZfpTcpAddress(
            string address,
            out string hostAndPort,
            out string? password,
            out bool passwordSupplied)
        {
            hostAndPort = address;
            password = null;
            passwordSupplied = false;

            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            var hostSeparator = address.IndexOf(':');
            if (hostSeparator < 0)
            {
                return true;
            }

            var passwordSeparator = address.IndexOf(':', hostSeparator + 1);
            var portEnd = passwordSeparator < 0 ? address.Length : passwordSeparator;
            var host = address.Substring(0, hostSeparator);
            var port = address.Substring(hostSeparator + 1, portEnd - hostSeparator - 1);
            if (host.Length == 0 || !int.TryParse(port, out _))
            {
                return false;
            }

            if (passwordSeparator >= 0)
            {
                hostAndPort = address.Substring(0, passwordSeparator);
                var passwordValue = address.Substring(passwordSeparator + 1);
                if (passwordValue.Length > 0)
                {
                    password = passwordValue;
                    passwordSupplied = true;
                }
            }

            return true;
        }

        /// <summary>
        /// <para>
        /// Connects to the fiscal printer with the specified device URI.
        /// 
        /// The Uri is in format "protocol://address". For example:
        /// </para>
        /// <para>
        /// bg.dy.json.http://printeraddress
        /// </para>
        /// </summary>
        /// <param name="deviceUri">The URI of the fiscal printer device.</param>
        /// <param name="autoDetect">While parsing the raw device info, driver tries to autodetect the protocol compliance.</param>
        /// <param name="options">Options for the printer.</param>
        /// <returns>
        /// The fiscal printer object.
        /// </returns>
        /// <exception cref="InvalidOperationException">When the printer is not found or the URI is not correctly formatted.</exception>
        public IFiscalPrinter Connect(string deviceUri, ServiceOptions serviceOptions, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            string protocol;
            string address;
            var isTremolTcpUri = TryGetTremolZfpTcpUriPrefix(deviceUri, out var tremolTcpProtocol, out var tremolTcpUriPrefix);
            if (isTremolTcpUri && !deviceUri.StartsWith(tremolTcpUriPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Malformed TREMOL TCP URI.");
            }

            if (isTremolTcpUri)
            {
                protocol = tremolTcpProtocol;
                address = deviceUri.Substring(tremolTcpUriPrefix.Length);
            }
            else
            {
                var match = uriPattern.Match(deviceUri);
                if (!match.Success)
                    throw new FormatException(
                        $"'{deviceUri}' is not recognized as valid device URI (protocol://address)");

                protocol = match.Groups["protocol"].Value;
                address = match.Groups["address"].Value;
            }

            FiscalPrinterDriver driver;
            Transport transport;
            try
            {
                (driver, transport) = protocols[protocol];
            }
            catch
            {
                throw new InvalidOperationException($"Unknown protocol '{protocol}'.");
            }

            IChannel channel;
            var tremolTcpPasswordSupplied = false;
            if (isTremolTcpUri
                && transport is TcpTransport tcpTransport)
            {
                if (!TrySplitTremolZfpTcpAddress(
                    address,
                    out var tcpAddress,
                    out var tcpPassword,
                    out tremolTcpPasswordSupplied))
                {
                    throw new FormatException("TREMOL TCP URI must contain a host and a valid port.");
                }

                channel = tcpTransport.OpenTremolChannel(
                    tcpAddress,
                    tremolTcpPasswordSupplied ? tcpPassword : null);
            }
            else
            {
                channel = transport.OpenChannel(address);
            }

            var uri = $"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}";
            var uriForLog = tremolTcpPasswordSupplied
                ? RedactDeviceUri(deviceUri) ?? "TREMOL TCP URI (address omitted)"
                : uri;
            try
            {
                var p = driver.Connect(channel, serviceOptions, autoDetect, options);
                p.DeviceInfo.Uri = uri;
                return p;
            }
            catch (Exception ex)
            {
                var error = tremolTcpPasswordSupplied ? ex.GetType().Name : ex.Message;
                Log.Error($"Cannot connect to {uriForLog}: {error}");
                throw;
            }
        }

        internal void InvalidateTremolZfpTcpChannel(string deviceUri)
        {
            if (!TryGetTremolZfpTcpUriPrefix(deviceUri, out var tremolProtocol, out _)
                || !TrySplitDeviceUriSecret(deviceUri, out var uriWithoutSecret, out var secret, out var secretSupplied)
                || !protocols.TryGetValue(tremolProtocol, out var protocol)
                || !(protocol.transport is TcpTransport tcpTransport))
            {
                return;
            }

            tcpTransport.InvalidateTremolChannel(
                uriWithoutSecret.Substring(uriWithoutSecret.IndexOf("://", StringComparison.Ordinal) + 3),
                secretSupplied ? secret : null);
        }

        private readonly Dictionary<string, (FiscalPrinterDriver driver, Transport transport)> protocols =
            new Dictionary<string, (FiscalPrinterDriver driver, Transport transport)>();

        public ServiceOptions ServiceOptions { get; }
    }
}
