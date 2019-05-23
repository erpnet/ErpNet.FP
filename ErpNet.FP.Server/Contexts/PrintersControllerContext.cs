using ErpNet.FP.Core;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Core.Transports;
using ErpNet.FP.Server.Configuration;
using ErpNet.FP.Server.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.FP.Server.Contexts
{
    public interface IPrintersControllerContext
    {
        Dictionary<string, DeviceInfo> PrintersInfo { get; }

        Dictionary<string, IFiscalPrinter> Printers { get; }

        public Task<object> RunAsync(
            IFiscalPrinter printer,
            PrintJobAction action,
            object? document,
            int asyncTimeout);

        public TaskInfoResult GetTaskInfo(string taskId);

        public bool IsReady { get; }
    }

    public class PrintersControllerContext : IPrintersControllerContext
    {
        private readonly ILogger logger;
        private Task? consumer;
        private readonly object taskSyncLock = new object();
        private readonly object consumerSyncLock = new object();
        private volatile bool isReady = false;

        public Provider Provider { get; } = new Provider();
        public Dictionary<string, DeviceInfo> PrintersInfo { get; } = new Dictionary<string, DeviceInfo>();

        public Dictionary<string, IFiscalPrinter> Printers { get; } = new Dictionary<string, IFiscalPrinter>();

        public ConcurrentQueue<string> TaskQueue { get; } = new ConcurrentQueue<string>();

        public ConcurrentDictionary<string, PrintJob> Tasks { get; } = new ConcurrentDictionary<string, PrintJob>();
        public bool IsReady { get => isReady; set => isReady = value; }

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

        public async Task<object> RunAsync(
            IFiscalPrinter printer,
            PrintJobAction action,
            object? document,
            int asyncTimeout)
        {
            var taskId = Enqueue(new PrintJob
            {
                Printer = printer,
                Document = document,
                Action = action
            });
            if (asyncTimeout == 0)
            {
                return new TaskIdResult { TaskId = taskId };
            }
            return await Task.Run(() => RunTask(taskId, asyncTimeout));
        }

        public object? RunTask(string taskId, int asyncTimeout)
        {
            const int timeoutMinimalStep = 50; // check the queue every 50 ms
            if (asyncTimeout < 0) asyncTimeout = PrintJob.DefaultTimeout;
            if (Tasks.TryGetValue(taskId, out PrintJob printJob))
            {
                // While the print job is not finished
                while (printJob.Result == null)
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
            // TODO: Clear Expired Tasks
            // ClearExpiredTasks();
            var taskId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Substring(0, 22)
                .Replace("/", "_")
                .Replace("+", "-");
            Tasks[taskId] = printJob;
            TaskQueue.Enqueue(taskId);
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
                duplicateNumber++;
                printerID = $"{baseID}_{duplicateNumber}";
            }
            PrintersInfo.Add(printerID, printer.DeviceInfo);
            Printers.Add(printerID, printer);
            logger.LogInformation($"Found {printerID}: {printer.DeviceInfo.Uri}");
        }

        public PrintersControllerContext(
            ILogger<PrintersControllerContext> logger,
            IWritableOptions<ErpNetFPConfigOptions> options)
        {
            this.logger = logger;

            var configOptions = options.Value;

            // Transports
            var comTransport = new ComTransport();

            // Drivers
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();
            var tremolV2Zfp = new BgTremolZfpV2FiscalPrinterDriver();

            // Add drivers and their compatible transports to the provider.
            var provider = new Provider()
                .Register(daisyIsl, comTransport)
                .Register(datecsCIsl, comTransport)
                .Register(datecsPIsl, comTransport)
                .Register(eltradeIsl, comTransport)
                .Register(datecsXIsl, comTransport)
                .Register(tremolZfp, comTransport)
                .Register(tremolV2Zfp, comTransport);

            var autoDetectedPrinters = new Dictionary<string, PrinterConfig>();

            if (configOptions.AutoDetect)
            {
                logger.LogInformation("Autodetecting local printers...");
                var printers = provider.DetectAvailablePrinters();
                foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
                {
                    AddPrinter(printer.Value);
                }
            }

            logger.LogInformation("Detecting configured printers...");
            //var printersSettings = configuration.GetSection("Printers").Get<Dictionary<string, PrinterConfig>>();
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
                            var printer = provider.Connect(printerSetting.Value.Uri, null);
                            logger.LogInformation($"{logString}, OK");
                            PrintersInfo.Add(printerSetting.Key, printer.DeviceInfo);
                            Printers.Add(printerSetting.Key, printer);
                        }
                        catch
                        {
                            logger.LogInformation($"{logString}, failed");
                            // Do not add this printer, it fails to connect.
                        }
                    }
                }

                // Auto save to config all listed printers, for future use
                // It is possible to have aliases, i.e. different PrinterId with the same Uri
                foreach (var printer in Printers)
                {
                    configOptions.Printers[printer.Key] = new PrinterConfig { Uri = printer.Value.DeviceInfo.Uri };
                }
            }

            configOptions.AutoDetect = Printers.Count == 0;

            options.Update(updatedConfigOptions =>
            {
                updatedConfigOptions.AutoDetect = configOptions.AutoDetect;
                updatedConfigOptions.Printers = configOptions.Printers ?? new Dictionary<string, PrinterConfig>();
            });

            logger.LogInformation($"Detecting done. Found {Printers.Count} available printer(s).");

            isReady = true;
        }
    }
}
