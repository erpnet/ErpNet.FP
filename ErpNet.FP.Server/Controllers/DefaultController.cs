using ErpNet.FP.Core;
using ErpNet.FP.Server.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;

namespace ErpNet.FP.Server.Controllers
{
    // Default controller, example: https://hostname
    [Route("")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        private readonly IPrintersControllerContext context;

        public DefaultController(IPrintersControllerContext context)
        {
            this.context = context;
        }

        // GET / 
        [HttpGet()]
        public ActionResult<Dictionary<string, DeviceInfo>> Admin()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "admin.html");

            return PhysicalFile(file, "text/html");
        }

        // GET admin/logo
        [HttpGet("admin/logo")]
        public ActionResult<Dictionary<string, DeviceInfo>> Logo()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "ErpNet.FP.thumb.png");

            return PhysicalFile(file, "image/png");
        }

        // GET admin/css
        [HttpGet("admin/css")]
        public ActionResult<Dictionary<string, DeviceInfo>> Css()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "admin.css");

            return PhysicalFile(file, "text/css");
        }

        // GET admin/js
        [HttpGet("admin/js")]
        public ActionResult<Dictionary<string, DeviceInfo>> JS()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "admin.js");

            return PhysicalFile(file, "text/javascript");
        }
    }
}
