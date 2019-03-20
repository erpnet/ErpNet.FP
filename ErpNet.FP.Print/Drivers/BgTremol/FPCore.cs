using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace TremolZFP
{
    public class FPcore
    {
        public readonly string VersionCore = "1.0.1.0";

        public XMode XmlMode = XMode.useArtificialXml;

        public bool useDebugOutput;

        private static Dictionary<string, XElement> dictXml = new Dictionary<string, XElement>();

        private string res;

        private IntPtr _errStrPtr = IntPtr.Zero;

        private static FPcore fp;

        private static int counter = 0;

        private static List<IntPtr> ptrs = new List<IntPtr>();

        public string ServerAddress
        {
            get;
            set;
        }

        public long VersionDef
        {
            get;
            protected set;
        }

        /// <summary>
        /// Executes command
        /// </summary>
        public XElement Do(string commandName, params object[] parameters)
        {
            if (XmlMode == XMode.useXmlDefs && !dictXml.Any())
            {
                loadXmlData();
            }
            XElement xElement = (XmlMode != XMode.useGetRequest) ? fillXmlWithParamVals(commandName, parameters) : concatGetMethodStr(commandName, parameters);
            if (ServerAddress == null)
            {
                throw new SException(SErrorType.ServerAddressNotSet, "Server Address Not Defined", null);
            }
            XElement xElement2 = null;
            try
            {
                if (useDebugOutput)
                {
                    Console.WriteLine("Request xml:\r\n" + xElement + "=================================");
                }
                xElement2 = ((XmlMode != XMode.useGetRequest) ? parseXml(sendXmlRequest(xElement)) : parseXml(ServerSendGETRequest(xElement.Value)));
                if (useDebugOutput)
                {
                    Console.WriteLine("Response xml:\r\n" + xElement2 + "\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n");
                }
            }
            catch (SException)
            {
                throw;
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ServerConnectionError, "Server Connection error", inner_ex);
            }
            if (xElement2 == null)
            {
                throw new SException(SErrorType.ServerResponseMissing, "Server response missing", null);
            }
            ThrowOnServerError(xElement2);
            return xElement2;
        }

        public static int Do2([In] [MarshalAs(UnmanagedType.LPWStr)] string strIn, out IntPtr[] strResPtrs, out IntPtr strErrPtr, out int STE1, out int STE2)
        {
            strErrPtr = IntPtr.Zero;
            strResPtrs = null;
            STE1 = 0;
            STE2 = 0;
            if (fp == null)
            {
                fp = new FPcore();
            }
            try
            {
                IEnumerable<XElement> source;
                if (strIn.Contains("*&"))
                {
                    int num = strIn.IndexOf("*&", StringComparison.Ordinal);
                    string text = strIn.Substring(0, num);
                    object[] array = strIn.Substring(num + 2).Split(';').ToArray();
                    string text2 = executeMethodByName(text, array);
                    if (!string.IsNullOrEmpty(text2))
                    {
                        strResPtrs = new IntPtr[1]
                        {
                        str2ptr(text2)
                        };
                        return 0;
                    }
                    if (array.Length % 2 == 1)
                    {
                        Array.Resize(ref array, array.Length - 1);
                    }
                    source = from o in fp.Do(text, array).Descendants("Res")
                             where o.Attribute("Type").Value != "OptionHardcoded"
                             select o;
                }
                else
                {
                    string text3 = executeMethodByName(strIn);
                    if (!string.IsNullOrEmpty(text3))
                    {
                        strResPtrs = new IntPtr[1]
                        {
                        str2ptr(text3)
                        };
                        return 0;
                    }
                    source = from o in fp.Do(strIn).Descendants("Res")
                             where o.Attribute("Type").Value != "OptionHardcoded"
                             select o;
                }
                if (source.Any())
                {
                    strResPtrs = (from r in source
                                  select str2ptr(r.Attribute("Value").Value)).ToArray();
                }
            }
            catch (Exception ex)
            {
                SException ex2 = ex as SException;
                if (ex2 != null)
                {
                    STE1 = ex2.STE1;
                    STE2 = ex2.STE2;
                    strErrPtr = str2ptr(ex2.Message);
                    return (int)ex2.ErrType;
                }
                strErrPtr = str2ptr(ex.Message);
                return 1;
            }
            return 0;
        }

        public static int TestExport(int left, int right)
        {
            return left + right;
        }

        public static void FreeMem()
        {
            foreach (IntPtr ptr in ptrs)
            {
                Marshal.FreeHGlobal(ptr);
            }
            ptrs.Clear();
        }

        private static IntPtr str2ptr(string s)
        {
            IntPtr intPtr = Marshal.StringToHGlobalUni(s);
            ptrs.Add(intPtr);
            return intPtr;
        }

        /// <summary>
        /// Creates result values corresponding of the result class
        /// </summary>
        public T CreateRes<T>(XElement xml)
        {
            try
            {
                List<XElement> list = xml.Descendants("Res").ToList();
                if (list.FirstOrDefault().Attribute("Value").Value == "@")
                {
                    return default(T);
                }
                IEnumerable<XElement> source = from o in list
                                               where o.Attribute("Type").Value != "OptionHardcoded"
                                               select o;
                if (source.Count() == 1)
                {
                    object value = source.FirstOrDefault().Attribute("Value").Value;
                    if (typeof(T) == typeof(string))
                    {
                        return (value is T) ? ((T)value) : default(T);
                    }
                    if (typeof(T) == typeof(decimal))
                    {
                        object obj = parseDecimal(value.ToString());
                        return (obj is T) ? ((T)obj) : default(T);
                    }
                    if (typeof(T) == typeof(DateTime))
                    {
                        object obj2 = DateTimeParse(value.ToString(), "dd-MM-yyyy HH:mm:ss");
                        return (obj2 is T) ? ((T)obj2) : default(T);
                    }
                    if (typeof(T) == typeof(byte[]))
                    {
                        object obj3 = Convert.FromBase64String(value.ToString());
                        return (obj3 is T) ? ((T)obj3) : default(T);
                    }
                    if (typeof(T) == typeof(Enum))
                    {
                        string text = value.ToString();
                        if (text.Length == 1)
                        {
                            object obj4 = Enum.ToObject(typeof(T), text[0]);
                            return (obj4 is T) ? ((T)obj4) : default(T);
                        }
                        object obj5 = Enum.ToObject(typeof(T), ConvertToInt(text));
                        return (obj5 is T) ? ((T)obj5) : default(T);
                    }
                }
                T val = Activator.CreateInstance<T>();
                if (!list.Any())
                {
                    return val;
                }
                PropertyInfo[] properties = typeof(T).GetProperties();
                foreach (PropertyInfo p in properties)
                {
                    List<XElement> source2 = list;
                    Func<XElement, bool> predicate = (XElement xe) => xe.Attribute("Name").Value == p.Name;
                    XElement xElement = source2.Single(predicate);
                    string theName = xElement.Attribute("Name").Value;
                    string value2 = xElement.Attribute("Value").Value;
                    string value3 = xElement.Attribute("Type").Value;
                    object value4 = default(T);
                    switch (value3)
                    {
                        case "DateTime":
                            value4 = DateTimeParse(value2, "dd-MM-yyyy HH:mm:ss");
                            break;
                        case "Flags":
                            value4 = byte.Parse(value2);
                            break;
                        case "Decimal_plus_80h":
                            value4 = int.Parse(value2);
                            break;
                        case "Decimal":
                        case "Decimal_with_format":
                            value4 = parseDecimal(value2);
                            break;
                        case "Base64":
                            value4 = Convert.FromBase64String(value2);
                            break;
                        case "OptionHardcoded":
                        case "Text":
                            value4 = value2;
                            break;
                        case "Status":
                            value4 = (value2 != "0");
                            break;
                        case "Option":
                            {
                                Type propertyType = typeof(T).GetProperties().FirstOrDefault(delegate (PropertyInfo pr)
                                {
                                    if (pr.PropertyType.IsEnum)
                                    {
                                        return pr.PropertyType.Name.StartsWith(theName);
                                    }
                                    return false;
                                }).PropertyType;
                                value4 = ((value2.Length == 1) ? Enum.ToObject(propertyType, value2[0]) : Enum.ToObject(propertyType, ConvertToInt(value2)));
                                break;
                            }
                    }
                    p.SetValue(val, value4, null);
                }
                return val;
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ClientCanNotParseResponseXML, "Unable to create result objects", inner_ex);
            }
        }

        /// <summary>
        /// Gets device serial port and TCP/IP communication settings.
        /// </summary>
        public string ServerGetSettings()
        {
            res = sendRequest("settings()");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Sets Device serial port communication settings.
        /// </summary>
        /// <param name="serialPort">Serial port</param>
        /// <param name="baudRate">Baud rate</param>
        public string ServerSetDeviceSerialPortSettings(string serialPort, int baudRate)
        {
            string request = $"settings(com={serialPort},baud={baudRate},tcp=0)";
            res = sendRequest(request);
            ThrowOnServerError(parseXml(res));
            try
            {
                checkVersion(res);
            }
            catch (SException ex)
            {
                if (ex.ErrType == SErrorType.ServerResponseError)
                {
                    Thread.Sleep(600);
                    res = sendRequest(request);
                    ThrowOnServerError(parseXml(res));
                    checkVersion(res);
                    goto end_IL_003f;
                }
                throw ex;
                end_IL_003f:;
            }
            return res;
        }

        /// <summary>
        /// Finds first device on serial port.
        /// </summary>
        /// <param name="serialPort">return device Serial port</param>
        /// <param name="baudRate">return device Baud rate</param>
        public bool ServerFindDevice(out string serialPort, out int baudRate)
        {
            XElement xElement = parseXml(sendRequest("finddevice()"));
            try
            {
                serialPort = xElement.Descendants("com").FirstOrDefault().Value;
                baudRate = int.Parse(xElement.Descendants("baud").FirstOrDefault().Value);
                if (string.IsNullOrEmpty(serialPort))
                {
                    return false;
                }
            }
            catch
            {
                serialPort = "";
                baudRate = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets Device TCP/IP communication settings.
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="tcpPort">TCP port</param>
        /// <param name="password">ZFP password</param>
        public string ServerSetDeviceTcpSettings(string ip, int tcpPort, string password)
        {
            IPAddress iPAddress = null;
            try
            {
                iPAddress = IPAddress.Parse(ip);
            }
            catch (Exception ex)
            {
                throw new Exception("Can not parse IP! " + ex.Message);
            }
            string request = $"settings(ip={iPAddress},port={tcpPort},password={password},tcp=1)";
            res = sendRequest(request);
            ThrowOnServerError(parseXml(res));
            try
            {
                checkVersion(res);
            }
            catch (SException ex2)
            {
                if (ex2.ErrType == SErrorType.ServerResponseError)
                {
                    Thread.Sleep(600);
                    res = sendRequest(request);
                    ThrowOnServerError(parseXml(res));
                    checkVersion(res);
                    goto end_IL_0062;
                }
                throw ex2;
                end_IL_0062:;
            }
            return res;
        }

        /// <summary>
        /// Sends request via GET method
        /// </summary>
        /// <param name="str">String request</param>
        public string ServerSendGETRequest(string str)
        {
            res = sendRequest(str);
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Disconnects from device - this method must be invoked when closing the app or when stop working with the server!
        /// </summary>
        public string ServerCloseDeviceConnection()
        {
            res = sendRequest("clientremove(who=me)");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Remote server restart 
        /// </summary>
        public string ServerRestart()
        {
            res = sendRequest("restart()");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Remote server stop 
        /// </summary>
        public string ServerStop()
        {
            res = sendRequest("exit()");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Set Log on or off, by default is ON
        /// </summary>
        /// <param name="on">
        /// on - true,
        /// off - false
        /// </param>
        public string ServerSetLog(bool on)
        {
            res = sendRequest($"log(on={(on ? 1 : 0)})");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Returns client log
        /// </summary>
        public string ServerGetLog()
        {
            res = sendRequest("log()");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Returns all clients connected to the server
        /// </summary>
        public string ServerGetClients()
        {
            res = sendRequest("clients()");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Disconnect all clients connected to the server
        /// </summary>
        public string ServerRemoveAllClients()
        {
            res = sendRequest("clientremove(who=all)");
            ThrowOnServerError(parseXml(res));
            return res;
        }

        /// <summary>
        /// Send xml directly to ZFP server
        /// </summary>
        public string SendRAWRequest(XElement xml)
        {
            try
            {
                res = sendXmlRequest(xml);
                ThrowOnServerError(parseXml(res));
                return res;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static string executeMethodByName(string mtdName, params object[] pars)
        {
            switch (mtdName)
            {
                case "ServerSetAddress":
                    fp.VersionDef = long.Parse(pars[1].ToString());
                    return fp.ServerAddress = pars[0].ToString();
                case "ServerGetSettings":
                    return fp.ServerGetSettings();
                case "ServerSetDeviceSerialPortSettings":
                    return fp.ServerSetDeviceSerialPortSettings(pars[0].ToString(), int.Parse(pars[1].ToString()));
                case "ServerSetDeviceTcpSettings":
                    return fp.ServerSetDeviceTcpSettings(pars[0].ToString(), int.Parse(pars[1].ToString()), pars[2].ToString());
                case "ServerCloseDeviceConnection":
                    return fp.ServerCloseDeviceConnection();
                case "ServerRestart":
                    return fp.ServerRestart();
                case "ServerSetLog":
                    return fp.ServerSetLog((bool)pars[0]);
                case "ServerGetLog":
                    return fp.ServerGetLog();
                case "ServerGetClients":
                    return fp.ServerGetClients();
                case "ServerRemoveAllClients":
                    return fp.ServerRemoveAllClients();
                default:
                    return "";
            }
        }

        private void ThrowOnServerError(XElement res)
        {
            int num = 0;
            string msg = "";
            int result = 0;
            int result2 = 0;
            int result3 = 0;
            try
            {
                num = int.Parse(res.Attribute("Code").Value);
                switch (num)
                {
                    case 0:
                        break;
                    case 40:
                        {
                            XElement xElement = res.Element("Err");
                            int.TryParse(xElement.Attribute("STE1").Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result);
                            int.TryParse(xElement.Attribute("STE2").Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result2);
                            int.TryParse(xElement.Attribute("FPLibErrorCode").Value, out result3);
                            goto default;
                        }
                    default:
                        {
                            XElement xElement2 = res.Descendants("Message").FirstOrDefault();
                            if (xElement2 != null)
                            {
                                msg = xElement2.Value;
                            }
                            break;
                        }
                }
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ServerResponseError, "Server response error ", inner_ex);
            }
            if (num == 0)
            {
                return;
            }
            if (Enum.IsDefined(typeof(SErrorType), num))
            {
                SException ex = new SException((SErrorType)num, msg, null);
                ex.STE1 = result;
                ex.STE2 = result2;
                ex.FPLibErrorCode = result3;
                throw ex;
            }
            throw new SException(SErrorType.ServerErr, msg, null);
        }

        private Dictionary<string, object> getParamValues(params object[] p)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            int num;
            for (num = 0; num < p.Count(); num++)
            {
                dictionary.Add(p[num].ToString(), p[num + 1]);
                num++;
            }
            return dictionary;
        }

        private XElement concatGetMethodStr(string commandName, params object[] parameters)
        {
            Dictionary<string, object> paramValues = getParamValues(parameters);
            string text = $"{commandName}(";
            foreach (KeyValuePair<string, object> item in paramValues)
            {
                string text2 = $"{item.Key}={item.Value}, ";
                if (paramValues.Last().Key == item.Key)
                {
                    text2 = text2.Replace(',', ')');
                }
                text += text2;
            }
            return new XElement("Command", text);
        }

        private XElement parseXml(string str)
        {
            try
            {
                return XElement.Parse(str);
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ServerResponseError, "Invalid response from server!", inner_ex);
            }
        }

        private XElement fillXmlWithParamVals(string commandName, params object[] parameters)
        {
            XElement xElement = null;
            Dictionary<string, object> paramValues = getParamValues(parameters);
            xElement = ((XmlMode != XMode.useArtificialXml) ? dictXml[commandName] : new XElement("Command", new XAttribute("Name", commandName), (!paramValues.Any()) ? null : new XElement("Args", paramValues.Select(delegate (KeyValuePair<string, object> p)
            {
                string value = (p.Value == null) ? string.Empty : p.Value.ToString();
                if (p.Value is byte[])
                {
                    value = Convert.ToBase64String((byte[])p.Value);
                }
                return new XElement("Arg", new XAttribute("Name", p.Key), new XAttribute("Value", value));
            }).ToArray())));
            XElement xElement2 = xElement.Element("Args");
            if (xElement2 != null && xElement2.HasElements)
            {
                {
                    foreach (XElement item in xElement2.Elements())
                    {
                        if (XmlMode != XMode.useArtificialXml)
                        {
                            XAttribute xAttribute = item.Attribute("Type");
                            if (xAttribute != null && !(xAttribute.Value == "OptionHardcoded"))
                            {
                                goto IL_0109;
                            }
                            continue;
                        }
                        goto IL_0109;
                        IL_0109:
                        string empty = string.Empty;
                        object obj = paramValues[item.Attribute("Name").Value];
                        empty = ((obj == null) ? "" : obj.ToString());
                        if (obj is DateTime)
                        {
                            empty = ((DateTime)obj).ToString("dd-MM-yyyy HH:mm:ss");
                        }
                        if (obj is Enum)
                        {
                            int num = (int)Enum.Parse(obj.GetType(), empty);
                            if (num <= 255 || num < 8192)
                            {
                                empty = ((char)(ushort)num).ToString();
                            }
                            else
                            {
                                empty = "";
                                for (int i = 0; i < 4; i++)
                                {
                                    int num2 = (num >> 8 * i) & 0xFF;
                                    if (num2 <= 0)
                                    {
                                        break;
                                    }
                                    empty += (char)num2;
                                }
                            }
                        }
                        if (obj is byte[])
                        {
                            empty = Convert.ToBase64String((byte[])obj);
                        }
                        item.SetAttributeValue("Value", empty);
                    }
                    return xElement;
                }
            }
            return xElement;
        }

        private string sendXmlRequest(XElement xml)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(ServerAddress);
            byte[] bytes = Encoding.UTF8.GetBytes(xml.ToString());
            httpWebRequest.ContentType = "text/xml; encoding='utf-8'";
            httpWebRequest.ContentLength = bytes.Length;
            httpWebRequest.Method = "POST";
            Stream requestStream = httpWebRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                Stream responseStream = httpWebResponse.GetResponseStream();
                return new StreamReader(responseStream).ReadToEnd();
            }
            return null;
        }

        private string sendRequest(string request)
        {
            try
            {
                var escapedRequest = HttpUtility.UrlEncode(request);
                var fullUrl = string.Concat(ServerAddress, escapedRequest);
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(fullUrl);
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = "application/text";
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = httpWebResponse.GetResponseStream();
                    return new StreamReader(responseStream).ReadToEnd();
                }
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ServerConnectionError, "Server Connection error", inner_ex);
            }
            return null;
        }

        private string loadXmlFile(string xmlName)
        {
            XDocument xDocument = XDocument.Load(xmlName);
            string value = xDocument.Root.FirstAttribute.Value;
            if (!dictXml.ContainsKey(value))
            {
                dictXml.Add(value, xDocument.Root);
            }
            return value;
        }

        private int ConvertToInt(string strVal)
        {
            int num = 0;
            for (int i = 0; i < strVal.Length; i++)
            {
                num += (byte)strVal[i] << 8 * i;
            }
            return num;
        }

        private string editNames(string name, string ch)
        {
            Regex regex = new Regex("[^а-яa-zA-Z0-9 ]");
            return regex.Replace(name, "").Replace(" ", ch);
        }

        private decimal parseDecimal(string val)
        {
            decimal dec = 0m;
            if (!tryParseDec(val, out dec))
            {
                return 0m;
            }
            return dec;
        }

        private static bool tryParseDec(string s, out decimal dec)
        {
            return decimal.TryParse(s.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out dec);
        }

        private DateTime DateTimeParse(string date, string format)
        {
            return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
        }

        private void checkVersion(string res)
        {
            XElement xElement = XElement.Parse(res).Descendants("defVer").FirstOrDefault();
            if (xElement != null)
            {
                long num = 0L;
                try
                {
                    num = long.Parse(xElement.Value);
                }
                catch
                {
                    num = VersionDef;
                    throw new SException(SErrorType.ServerResponseError, "Unable to get version of server definitions. Server not initialized!", null);
                }
                if (num < VersionDef)
                {
                    throw new SException(SErrorType.ServerDefsMismatch, "Server definitions are older than yours", null);
                }
                if (num <= VersionDef)
                {
                    return;
                }
                throw new SException(SErrorType.ServerDefsMismatch, "Server definitions are newer than yours", null);
            }
            throw new SException(SErrorType.ServerResponseError, "Unable to get version of server definitions. Tag missing!", null);
        }

        private void loadXmlData()
        {
            string text = string.Empty;
            try
            {
                string path = string.Format(Environment.CurrentDirectory + "\\xml\\");
                dictXml = new Dictionary<string, XElement>();
                List<FileInfo> source = (from file in Directory.GetFiles(path, "*.xml")
                                         select new FileInfo(file)).ToList();
                foreach (FileInfo item in source.Where(delegate (FileInfo f)
                {
                    if (!f.Name.StartsWith("s_") && !f.Name.StartsWith("_"))
                    {
                        return f.Name != "menu.xml";
                    }
                    return false;
                }))
                {
                    text = item.FullName;
                    loadXmlFile(text);
                }
            }
            catch (Exception inner_ex)
            {
                throw new SException(SErrorType.ClientXMLCanNotParse, $"Can't load XML file - {text}", inner_ex);
            }
        }
    }

    public enum XMode
    {
        useXmlDefs,
        useArtificialXml,
        useGetRequest
    }

    public enum SErrorType
    {
        OK,
        ServMismatchBetweenDefinitionAndFPResult = 9,
        ServDefMissing,
        ServArgDefMissing,
        ServCreateCmdString,
        ServUndefined = 19,
        ServSockConnectionFailed = 30,
        ServTCPAuth,
        ServWrongTcpConnSettings,
        ServWrongSerialPortConnSettings,
        ServWaitOtherClientCmdProcessingTimeOut,
        ServDisconnectOtherClientErr,
        FPException = 40,
        ClientArgDefMissing = 50,
        ClientAttrDefMissing,
        ClientArgValueWrongFormat,
        ClientSettingsNotInitialized,
        ClientInvalidGetFormat = 62,
        ClientInvalidPostFormat,
        ServerAddressNotSet = 100,
        ServerConnectionError,
        ServerResponseMissing,
        ServerResponseError,
        ServerDefsMismatch,
        ClientXMLCanNotParse,
        ClientCanNotParseResponseXML,
        ServerErr = 1000
    }


    /// <summary>
    /// SException inheritance of Exception Class
    /// </summary>
    public class SException : Exception
    {
        /// <summary>
        /// Server Error Type
        /// </summary>
        public SErrorType ErrType;

        private string msg_internal = "";

        private Exception inner_ex;

        /// <summary>
        /// Error Status Byte 1 - contain FP error code
        /// </summary>
        public int STE1
        {
            get;
            set;
        }

        /// <summary>
        /// Error Status Byte 2 - contain FP command error code
        /// </summary>
        public int STE2
        {
            get;
            set;
        }

        /// <summary>
        /// Get/Set  FPLib Error Code
        /// </summary>
        public int FPLibErrorCode
        {
            get;
            set;
        }

        public override string Message
        {
            get
            {
                string text = (inner_ex != null) ? inner_ex.Message : "";
                return "ErrCode: " + (int)ErrType + " - " + ErrType + "\n" + msg_internal + "\n" + text;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SException class with specified error type, error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="err_type">Returns a number corresponding to one of the error values. </param>
        /// <param name="msg">The error message that explains the reason for the exception. </param>
        /// <param name="inner_ex">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified. </param>
        public SException(SErrorType err_type, string msg, Exception inner_ex)
        {
            ErrType = err_type;
            msg_internal = msg;
            this.inner_ex = inner_ex;
        }

        /// <summary>
        /// Returns true, if the exception is caused in FP device 
        /// </summary>
        /// <returns>Bool value</returns>
        public bool IsFiscalPrinterError()
        {
            return ErrType == SErrorType.FPException;
        }
    }


}
