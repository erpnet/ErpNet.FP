using ErpNet.FP.Core;
using ErpNet.FP.Win.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ErpNet.FP.Win.Contexts;

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
            if (!context.PrintersInfo.ContainsKey(id))
            {
                return NotFound();
            }
            return context.PrintersInfo[id];
        }

        // GET printers/{id}/status
        [HttpGet("{id}/status")]
        public ActionResult<DeviceStatus> Status(string id)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            return context.Printers[id].CheckStatus();
        }

        // POST printers/{id}/printreceipt
        [HttpPost("{id}/printreceipt")]
        public ActionResult<PrintReceiptResult> PrintReceipt(string id, [FromBody] Receipt receipt)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            var (info, status) = context.Printers[id].PrintReceipt(receipt);
            return new PrintReceiptResult { Info = info, Status = status };
        }

        // POST printers/{id}/printreversalreceipt
        [HttpPost("{id}/printreversalreceipt")]
        public ActionResult<PrintResult> PrintReversalReceipt(string id, [FromBody] ReversalReceipt reversalReceipt)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            return new PrintResult { Status = context.Printers[id].PrintReversalReceipt(reversalReceipt) };
        }

        // POST printers/{id}/printwithdraw
        [HttpPost("{id}/printwithdraw")]
        public ActionResult<PrintResult> PrintWithdraw(string id, [FromBody] TransferAmount withdraw)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            return new PrintResult { Status = context.Printers[id].PrintMoneyWithdraw(withdraw.Amount) };
        }

        // POST printers/{id}/printdeposit
        [HttpPost("{id}/printdeposit")]
        public ActionResult<PrintResult> PrintDeposit(string id, [FromBody] TransferAmount deposit)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            return new PrintResult { Status = context.Printers[id].PrintMoneyDeposit(deposit.Amount) };
        }

        // POST printers/{id}/printzeroingreport
        [HttpPost("{id}/printzeroingreport")]
        public ActionResult<PrintResult> PrintZeroingReport(string id)
        {
            if (!context.Printers.ContainsKey(id))
            {
                return NotFound();
            }
            return new PrintResult { Status = context.Printers[id].PrintZeroingReport() };
        }
    }
}
