using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ErpNet.FP.Core;
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

        public DefaultController (IPrintersControllerContext context) {
            this.context = context;

            var assembly = Assembly.GetExecutingAssembly ();
            serverVariables.Version = assembly.GetName ().Version.ToString ();
            serverVariables.ServerId = context.ServerId;
        }

        // GET / 
        [HttpGet ()]
        public ActionResult<Dictionary<string, DeviceInfo>> Admin () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "admin.html");

            return PhysicalFile (file, "text/html");
        }

        // GET vars
        [HttpGet ("vars")]
        public ActionResult<ServerVariables> Vars () {
            return serverVariables;
        }

        // GET admin/logo
        [HttpGet ("admin/logo")]
        public ActionResult<Dictionary<string, DeviceInfo>> Logo () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "ErpNet.FP.thumb.png");

            return PhysicalFile (file, "image/png");
        }

        // GET admin/logo
        [HttpGet ("admin/favicon.ico")]
        public ActionResult<Dictionary<string, DeviceInfo>> Favicon () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "ErpNet.FP.ico");

            return PhysicalFile (file, "image/x-icon");
        }

        // GET admin/css
        [HttpGet ("admin/css")]
        public ActionResult<Dictionary<string, DeviceInfo>> Css () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "admin.css");

            return PhysicalFile (file, "text/css");
        }

        // GET admin/js
        [HttpGet ("admin/js")]
        public ActionResult<Dictionary<string, DeviceInfo>> JS () {
            var file = Path.Combine (Directory.GetCurrentDirectory (), "admin.js");

            return PhysicalFile (file, "text/javascript");
        }
    }
}