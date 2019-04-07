using Microsoft.AspNetCore.Mvc;

namespace ErpNet.FP.Win.Controllers
{
    // Default controller, example: https://hostname
    [Route("")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        // GET json api definition
        [HttpGet()]
        public ActionResult<string> JsonAPI()
        {
            // TODO: gets the json api definition of Printers Controller
            return string.Empty;
        }
    }
}
