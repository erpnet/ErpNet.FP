using Microsoft.AspNetCore.Mvc;

namespace ErpNet.FP.Server.Controllers
{
    // Default controller, example: https://hostname
    [Route("")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        // GET json api definition
        [HttpGet()]
        public ActionResult ApiDocumentation()
        {
            return RedirectPermanent("https://documenter.getpostman.com/view/6751288/S1EJYMg5");
        }
    }
}
