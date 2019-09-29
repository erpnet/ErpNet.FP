namespace ErpNet.FP.Server.Controllers
{
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
        }


        // GET vars
        [HttpGet("vars")]
        public ActionResult<ServerVariables> Vars()
        {
            return serverVariables;
        }

        // GET toggleautodetect
        [HttpGet("toggleautodetect")]
        public ActionResult<ServerVariables> ToggleAutoDetect()
        {
            context.AutoDetect = !context.AutoDetect;
            serverVariables.AutoDetect = context.AutoDetect;
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

        // GET printers
        [HttpGet("printers")]
        public ActionResult<Dictionary<string, PrinterConfig>> Configured()
        {
            return context.ConfiguredPrinters;
        }

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