using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ErpNet.FP.Win.Models;

namespace ErpNet.FP.Win.Controllers
{
    // Default controller FPController, example: https://hostname/fp/[controller]
    [Route("")]
    [ApiController]
    public class FPController : ControllerBase
    {
        // POST fp/status
        [HttpPost("status")]
        public ActionResult<PrintResult> Post([FromBody] PrinterStatus printer)
        {
            return new PrintResult();
        }

        // POST fp/info
        [HttpPost("info")]
        public ActionResult<PrinterInfoResult> Post([FromBody] PrinterInfo printer)
        {
            return new PrinterInfoResult();
        }

        // POST fp/printreceipt
        [HttpPost("printreceipt")]
        public ActionResult<PrintReceiptResult> Post([FromBody] PrintReceipt receipt)
        {
            return new PrintReceiptResult();
        }

        // POST fp/printreversalreceipt
        [HttpPost("printreversalreceipt")]
        public ActionResult<PrintResult> Post([FromBody] PrintReversalReceipt reversalReceipt)
        {
            return new PrintResult();
        }

        // POST fp/printwithdraw
        [HttpPost("printwithdraw")]
        public ActionResult<PrintResult> Post([FromBody] PrintWithdraw withdraw)
        {
            return new PrintResult();
        }

        // POST fp/printdeposit
        [HttpPost("printdeposit")]
        public ActionResult<PrintResult> Post([FromBody] PrintDeposit deposit)
        {
            return new PrintResult();
        }
    }
}
