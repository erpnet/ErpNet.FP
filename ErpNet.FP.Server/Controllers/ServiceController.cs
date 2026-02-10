namespace ErpNet.FP.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ErpNet.FP.Core.Configuration;
    using ErpNet.FP.Core.Service;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Hosting;

    // PrintersController, example: //host/service/[controller]
    [Route("[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceController context;
        private readonly IHostApplicationLifetime serviceLifeTime;
        private readonly ServerVariables serverVariables = new ServerVariables();

        public ServiceController(IServiceController context, IHostApplicationLifetime serviceLifeTime)
        {
            this.context = context;
            this.serviceLifeTime = serviceLifeTime;

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            serverVariables.Version = (version != null) ? version.ToString() : "unknown";
            serverVariables.ServerId = context.ServerId;
            serverVariables.AutoDetect = context.AutoDetect;
            serverVariables.UdpBeaconPort = context.UdpBeaconPort;
            serverVariables.ExcludePortList = context.ExcludePortList;
            serverVariables.DetectionTimeout = context.DetectionTimeout;
            serverVariables.WebAccess = context.WebAccess;
        }

        // GET vars
        [HttpGet("vars")]
        public ActionResult<ServerVariables> Vars()
            => serverVariables;

        // GET toggleautodetect
        [HttpGet("toggleautodetect")]
        public ActionResult<ServerVariables> ToggleAutoDetect()
        {
            context.AutoDetect = !context.AutoDetect;
            serverVariables.AutoDetect = context.AutoDetect;
            return serverVariables;
        }

        // POST excludeports
        [HttpPost("excludeports")]
        public ActionResult<ServerVariables> ExcludePorts(Dictionary<string, string> param) 
        {
            //context.AutoDetect = !context.AutoDetect;
            string? newValue;
            if (param.TryGetValue("ExcludePortList", out newValue))
            {
                var excludePortArray = newValue.ToUpper()
                        .Split(new char[] { ',', ';', ':', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
                var excludePortString = string.Join(", ", excludePortArray);
                serverVariables.ExcludePortList = excludePortString;
                context.ExcludePortList = excludePortString;
            }
            return serverVariables;
        }

        // POST detectiontimeout
        [HttpPost("detectiontimeout")]
        public ActionResult<ServerVariables> DetectionTimeout(Dictionary<string, string> param)
        {
            string? newValue;
            if (param.TryGetValue("DetectionTimeout", out newValue))
            {
                if (int.TryParse(newValue, out int value))
                {
                    if (value > 0)
                    {
                        serverVariables.DetectionTimeout = value;
                        context.DetectionTimeout = value;
                    }
                }
            }
            return serverVariables;
        }

        // GET detect
        [HttpGet("detect")]
        public ActionResult Detect()
        {
            if (context.Detect(true))
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            return StatusCode(StatusCodes.Status423Locked);
        }

        // GET stop
        [HttpGet("stop")]
        public ActionResult Stop()
        {
            try
            {
                serviceLifeTime.StopApplication();
                return StatusCode(StatusCodes.Status200OK);
            }
            catch
            {
                return StatusCode(StatusCodes.Status423Locked);
            }
        }

        // GET printersprops
        [HttpGet("printersprops")]
        public ActionResult<Dictionary<string, PrinterProperties>> GetPrinterSpecificProperties()
            => context.PrintersProperties;

        // POST printersprops
        [HttpPost("printersprops")]
        public ActionResult<Dictionary<string, PrinterProperties>> PrinterSpecificProperties(Dictionary<string, PrinterProperties> printersProperties)
        {
            context.PrintersProperties = printersProperties;
            return context.PrintersProperties;
        }

        [HttpPost("webaccess")]
        public ActionResult<ServerVariables> UpdateWebAccess([FromBody] WebAccessOptions newWebAccess)
        {
            if (newWebAccess == null)
                return BadRequest("Invalid configuration data.");

            context.WebAccess = newWebAccess;
            serverVariables.WebAccess = context.WebAccess;

            return Ok(serverVariables);
        }

        // GET printers
        [HttpGet("printers")]
        public ActionResult<Dictionary<string, PrinterConfig>> Configured()
            => context.ConfiguredPrinters;

        // POST printers/configure
        [HttpPost("printers/configure")]
        public ActionResult Configure(
            [FromBody] PrinterConfigWithId printerConfigWithId
        )
        {
            if (context.ConfigurePrinter(printerConfigWithId))
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            return StatusCode(StatusCodes.Status423Locked);
        }

        // POST printers/delete
        [HttpPost("printers/delete")]
        public ActionResult Delete(
            [FromBody] PrinterConfigWithId printerConfigWithId
        )
        {
            if (context.DeletePrinter(printerConfigWithId))
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            return StatusCode(StatusCodes.Status423Locked);
        }
    }
}