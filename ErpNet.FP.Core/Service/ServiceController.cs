using ErpNet.FP.Core.Configuration;
using System;
using ErpNet.FP.Core.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.FP.Core.Service
{
    public interface IServiceController
    {
        Dictionary<string, DeviceInfo> PrintersInfo { get; }

        Dictionary<string, IFiscalPrinter> Printers { get; }

        Dictionary<string, PrinterConfig> ConfiguredPrinters { get; }

        Task<object> RunAsync(PrintJob printJob);

        TaskInfoResult GetTaskInfo(string taskId);

        bool Detect(bool forceAutoDetect = false);

        bool IsReady { get; }

        string ServerId { get; }

        bool AutoDetect { get; set; }

        bool ConfigurePrinter(PrinterConfigWithId printerConfigWithId);

        bool DeletePrinter(PrinterConfigWithId printerConfigWithId);
    }

    public abstract class ServiceControllerContext : IServiceController
    {

        private Task? consumer;
        private readonly object taskSyncLock = new object();
        private readonly object consumerSyncLock = new object();
        protected volatile bool isReady = false;

        protected ServiceOptions configOptions = new ServiceOptions();
        public string ServerId { get; private set; } = string.Empty;
        public Provider.Provider Provider { get; protected set; } = new Provider.Provider();
        public Dictionary<string, DeviceInfo> PrintersInfo { get; } = new Dictionary<string, DeviceInfo>();
        public Dictionary<string, IFiscalPrinter> Printers { get; } = new Dictionary<string, IFiscalPrinter>();
        public Dictionary<string, PrinterConfig> ConfiguredPrinters
        {
            get
            {
                return configOptions.Printers;
            }
        }
        public ConcurrentQueue<string> TaskQueue { get; } = new ConcurrentQueue<string>();
        public ConcurrentDictionary<string, PrintJob> Tasks { get; } = new ConcurrentDictionary<string, PrintJob>();
        public bool IsReady { get => isReady; set => isReady = value; }

        public bool AutoDetect
        {
            get => configOptions.AutoDetect;

            set
            {
                configOptions.AutoDetect = value;
                WriteOptions();
            }
        }

        protected abstract void SetupProvider();
        protected abstract void WriteOptions();

        protected virtual void Setup()
        {
            ReadOptions();
            SetupProvider();
            isReady = true;
            Task.Run(() => Detect());
        }

        protected virtual void ReadOptions()
        {
            // Server ID
            if (String.IsNullOrEmpty(configOptions.ServerId))
            {
                // serverId is RFC7515 Guid
                var serverId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Substring(0, 22)
                    .Replace("/", "_")
                    .Replace("+", "-");
                configOptions.ServerId = serverId;
            }
            this.ServerId = configOptions.ServerId;
        }

        public bool Detect(bool forceAutoDetect = false)
        {
            lock (taskSyncLock)
            {
                if (TaskQueue.Count == 0 && isReady)
                {
                    isReady = false;

                    PrintersInfo.Clear();
                    Printers.Clear();

                    // Autodetecting
                    var autoDetectedPrinters = new Dictionary<string, PrinterConfig>();
                    if (forceAutoDetect || configOptions.AutoDetect)
                    {
                        Log.Information("Autodetecting local printers...");
                        var printers = Provider.DetectAvailablePrinters();
                        foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
                        {
                            AddPrinter(printer.Value);
                        }
                    }

                    // Detecting configured printers
                    Log.Information("Detecting configured printers...");
                    if (configOptions.Printers != null)
                    {
                        foreach (var printerSetting in configOptions.Printers)
                        {
                            string logString = $"Trying {printerSetting.Key}: {printerSetting.Value.Uri}";
                            var uri = printerSetting.Value.Uri;
                            if (uri.Length > 0)
                            {
                                try
                                {
                                    var printer = Provider.Connect(printerSetting.Value.Uri, false, null);
                                    Log.Information($"{logString}, OK");
                                    PrintersInfo.Add(printerSetting.Key, printer.DeviceInfo);
                                    Printers.Add(printerSetting.Key, printer);
                                }
                                catch
                                {
                                    Log.Information($"{logString}, failed");
                                    // Do not add this printer, it fails to connect.
                                }
                            }
                        }

                        /*
                        // Auto cache to config all listed printers, for future use
                        // It is possible to have aliases, i.e. different PrinterId with the same Uri
                        foreach (var printer in Printers)
                        {
                            configOptions.Printers[printer.Key] = new PrinterConfig { Uri = printer.Value.DeviceInfo.Uri };
                        }
                        */
                    }

                    // configOptions.AutoDetect = Printers.Count == 0;

                    WriteOptions();

                    Log.Information($"Detecting done. Found {Printers.Count} available printer(s).");

                    isReady = true;

                    return true;
                }
                return false;
            }
        }

        public TaskInfoResult GetTaskInfo(string taskId)
        {
            lock (taskSyncLock)
            {
                var taskInfoResult = new TaskInfoResult();
                {
                    if (Tasks.TryGetValue(taskId, out PrintJob printJob))
                    {
                        taskInfoResult.TaskStatus = printJob.TaskStatus;
                        if (printJob.Result != null)
                        {
                            taskInfoResult.Result = printJob.Result;
                        }
                    }
                }
                return taskInfoResult;
            }
        }

        public async Task<object> RunAsync(PrintJob printJob)
        {
            try
            {
                var taskId = Enqueue(printJob);

                if (printJob.AsyncTimeout == 0)
                {
                    return new TaskIdResult { TaskId = taskId };
                }
                return await Task.Run(() => RunTask(taskId, printJob.AsyncTimeout));
            }
            catch (StandardizedStatusMessageException e)
            {
                var status = new DeviceStatus();
                status.AddError(e.Code, e.Message);
                return status;
            }
        }

        public object? RunTask(string taskId, int asyncTimeout)
        {
            const int timeoutMinimalStep = 50; // check the queue every 50 ms
            if (asyncTimeout < 0) asyncTimeout = PrintJob.DefaultTimeout;
            if (Tasks.TryGetValue(taskId, out PrintJob printJob))
            {
                // While the print job is not finished
                while (printJob.Finished == null)
                {
                    // We give the device some time to process the job
                    Thread.Sleep(timeoutMinimalStep);
                    asyncTimeout -= timeoutMinimalStep;
                    if (asyncTimeout <= 0) // Async timeout occured, so return taskId
                    {
                        return new TaskIdResult { TaskId = taskId };
                    }
                }
                return printJob.Result;
            }
            else
            {
                return null;
            }
        }

        private void EnsureConsumer()
        {
            lock (consumerSyncLock)
            {
                if (consumer == null || consumer.IsCompleted || consumer.IsFaulted)
                {
                    consumer = Task.Factory.StartNew(() => ConsumeTaskQueue(), TaskCreationOptions.LongRunning);
                }
            }
        }

        public void ConsumeTaskQueue()
        {
            // Run all tasks from the TaskQueue
            while (TaskQueue.TryDequeue(out string taskId))
            {
                // Resolve printJob by taskId
                if (Tasks.TryGetValue(taskId, out PrintJob printJob))
                {
                    printJob.Run();
                }
            }
        }

        public string Enqueue(PrintJob printJob)
        {
            // CASE: Clearing Expired Tasks
            // ClearExpiredTasks();

            // taskId is RFC7515 Guid
            var taskId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Substring(0, 22)
                .Replace("/", "_")
                .Replace("+", "-");

            if (!string.IsNullOrEmpty(printJob.TaskId))
            {
                // override generated taskId with user supplied taskId
                taskId = printJob.TaskId;

                // maximum length of 60 characters validation
                if (taskId.Length > 60)
                {
                    throw new StandardizedStatusMessageException($"Task Id is too long", "E110");
                }

                // printable characters validation
                var nonPrintableCharactersPattern = @"[\x01-\x1F]";
                Match match = Regex.Match(taskId, nonPrintableCharactersPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    throw new StandardizedStatusMessageException($"Use only printable charaters for Task Id", "E110");
                }
            }

            if (Tasks.ContainsKey(taskId))
            {
                throw new StandardizedStatusMessageException($"Task Id \"{taskId}\" is already used", "E109");
            }

            Tasks[taskId] = printJob;
            TaskQueue.Enqueue(taskId);

            // Ensure that there is a running ConsumerThread, that will process the queue
            EnsureConsumer();

            return taskId;
        }

        public void AddPrinter(IFiscalPrinter printer)
        {
            // We use serial number of local connected fiscal printers as Printer ID
            var baseID = printer.DeviceInfo.SerialNumber.ToLowerInvariant();

            var printerID = baseID;
            int duplicateNumber = 0;
            while (PrintersInfo.ContainsKey(printerID))
            {
                if (PrintersInfo[printerID].Uri == printer.DeviceInfo.Uri)
                {
                    // Do not add enumeration for duplicated Id and Uri
                    return;
                }
                duplicateNumber++;
                printerID = $"{baseID}_{duplicateNumber}";
            }
            PrintersInfo.Add(printerID, printer.DeviceInfo);
            Printers.Add(printerID, printer);
            Log.Information($"Found {printerID}: {printer.DeviceInfo.Uri}");
        }

        public bool ConfigurePrinter(PrinterConfigWithId printerConfigWithId)
        {
            if (String.IsNullOrEmpty(printerConfigWithId.Id) || String.IsNullOrEmpty(printerConfigWithId.Uri))
            {
                return false;
            }
            lock (taskSyncLock)
            {
                ConfiguredPrinters.Add(printerConfigWithId.Id, printerConfigWithId);
                WriteOptions();
                return true;
            }
        }

        public bool DeletePrinter(PrinterConfigWithId printerConfigWithId)
        {
            if (String.IsNullOrEmpty(printerConfigWithId.Id))
            {
                return false;
            }
            lock (taskSyncLock)
            {
                if (!ConfiguredPrinters.Remove(printerConfigWithId.Id))
                {
                    return false;
                }
                WriteOptions();
                return true;
            }
        }
    }
}
