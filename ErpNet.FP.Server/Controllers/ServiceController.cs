using ErpNet.FP.Core.Configuration;
using ErpNet.FP.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Reflection;

namespace ErpNet.FP.Server.Controllers
{
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
            serverVariables.Version = assembly.GetName().Version.ToString();
            serverVariables.ServerId = context.ServerId;
        }

        // GET vars
        [HttpGet("vars")]
        public ActionResult<ServerVariables> Vars()
        {
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