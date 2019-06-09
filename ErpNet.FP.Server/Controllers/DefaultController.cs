using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ErpNet.FP.Core;
using ErpNet.FP.Server.Configuration;
using ErpNet.FP.Server.Contexts;
using ErpNet.FP.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace ErpNet.FP.Server.Controllers {
    // Default controller, example: https://hostname
    [Route ("")]
    [ApiController]
    public class DefaultController : ControllerBase {
        private readonly IPrintersControllerContext context;
        private readonly ServerVariables serverVariables = new ServerVariables ();
        private readonly IDictionary<string, string> AvailableFiles = new Dictionary<string, string>();

        public DefaultController (IPrintersControllerContext context) {
            this.context = context;

            var assembly = Assembly.GetExecutingAssembly ();
            serverVariables.Version = assembly.GetName ().Version.ToString ();
            serverVariables.ServerId = context.ServerId;

            AvailableFiles.Add("index.css", "text/css");
            AvailableFiles.Add("index.js", "text/javascript");
            AvailableFiles.Add("ErpNet.FP.thumb.png", "image/png");
            AvailableFiles.Add("favicon.ico", "image/x-icon");
        }

        // GET / 
        [HttpGet ()]
        public ActionResult<Dictionary<string, DeviceInfo>> Admin () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "index.html");
            return PhysicalFile (file, "text/html");
        }

        // GET vars
        [HttpGet ("vars")]
        public ActionResult<ServerVariables> Vars () {
            return serverVariables;
        }

        // GET configured
        [HttpGet("configured")]
        public ActionResult<Dictionary<string, PrinterConfig>> Configured()
        {
            return context.ConfiguredPrinters;
        }

        // GET file/{id}
        [HttpGet ("file/{id}")]
        public ActionResult<Dictionary<string, DeviceInfo>> File (string id) {
            if (AvailableFiles.TryGetValue(id, out string mimeType))
            {
                var file = Path.Combine(Directory.GetCurrentDirectory(), id);
                return PhysicalFile(file, mimeType);
            }
            return NotFound();
        }
    }
}