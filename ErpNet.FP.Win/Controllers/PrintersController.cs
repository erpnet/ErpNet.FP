using ErpNet.FP.Core;
using ErpNet.FP.Win.Contexts;
using ErpNet.FP.Win.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ErpNet.FP.Win.Controllers
{
    // PrintersController, example: https://hostname/printers/[controller]
    [Route("[controller]")]
    [ApiController]
    public class PrintersController : ControllerBase
    {
        private readonly IPrintersControllerContext context;

        public PrintersController(IPrintersControllerContext context)
        {
            this.context = context;
        }

        // GET printers
        [HttpGet()]
        public ActionResult<Dictionary<string, DeviceInfo>> Printers()
        {
            return context.PrintersInfo;
        }

        // GET printers/{id}
        [HttpGet("{id}")]
        public ActionResult<DeviceInfo> Info(string id)
        {
            try
            {
                return context.PrintersInfo[id];
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // GET printers/{id}/status
        [HttpGet("{id}/status")]
        public ActionResult<DeviceStatusEx> Status(string id)
        {
            try
            {
                return context.Printers[id].CheckStatus();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST printers/{id}/printreceipt
        [HttpPost("{id}/printreceipt")]
        public ActionResult<PrintReceiptResult> PrintReceipt(string id, [FromBody] Receipt receipt)
        {
            try
            {
                var (info, status) = context.Printers[id].PrintReceipt(receipt);
                return new PrintReceiptResult { Info = info, Status = status };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST printers/{id}/printreversalreceipt
        [HttpPost("{id}/printreversalreceipt")]
        public ActionResult<PrintResult> PrintReversalReceipt(string id, [FromBody] ReversalReceipt reversalReceipt)
        {
            try
            {
                return new PrintResult {
                    Status = context.Printers[id].PrintReversalReceipt(reversalReceipt)
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            
        }

        // POST printers/{id}/printwithdraw
        [HttpPost("{id}/printwithdraw")]
        public ActionResult<PrintResult> PrintWithdraw(string id, [FromBody] TransferAmount withdraw)
        {
            try
            {
                return new PrintResult
                {
                    Status = context.Printers[id].PrintMoneyWithdraw(withdraw.Amount)
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST printers/{id}/printdeposit
        [HttpPost("{id}/printdeposit")]
        public ActionResult<PrintResult> PrintDeposit(string id, [FromBody] TransferAmount deposit)
        {
            try
            {
                return new PrintResult
                {
                    Status = context.Printers[id].PrintMoneyDeposit(deposit.Amount)
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST printers/{id}/setdatetime
        [HttpPost("{id}/setdatetime")]
        public ActionResult<PrintResult> SetDateTime(string id, [FromBody] CurrentDateTime currentDateTime)
        {
            try
            {
                return new PrintResult
                {
                    Status = context.Printers[id].SetDateTime(currentDateTime.DateTime)
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST printers/{id}/printzeroingreport
        [HttpPost("{id}/printzeroingreport")]
        public ActionResult<PrintResult> PrintZeroingReport(string id)
        {
            try
            {
                return new PrintResult
                {
                    Status = context.Printers[id].PrintZeroingReport()
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
