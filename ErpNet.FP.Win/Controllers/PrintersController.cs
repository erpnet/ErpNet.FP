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
        public ActionResult<DeviceInfo> Info(string id) =>
            context.PrintersInfo.TryGetValue(id, out DeviceInfo deviceInfo)
            ?
            (ActionResult<DeviceInfo>)deviceInfo
            :
            NotFound();

        // GET printers/{id}/status
        [HttpGet("{id}/status")]
        public ActionResult<DeviceStatusEx> Status(string id) =>
            context.Printers.TryGetValue(id, out IFiscalPrinter printer)
            ?
            (ActionResult<DeviceStatusEx>)printer.CheckStatus()
            :
            NotFound();

        // POST printers/{id}/receipt
        [HttpPost("{id}/receipt")]
        public ActionResult<PrintReceiptResult> PrintReceipt(string id, [FromBody] Receipt receipt)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var (info, status) = printer.PrintReceipt(receipt);
                return new PrintReceiptResult { Info = info, Status = status };
            }
            return NotFound();
        }

        // POST printers/{id}/reversalreceipt
        [HttpPost("{id}/reversalreceipt")]
        public ActionResult<PrintResult> PrintReversalReceipt(string id, [FromBody] ReversalReceipt reversalReceipt)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.PrintReversalReceipt(reversalReceipt)
                };
            }
            return NotFound();
        }

        // POST printers/{id}/withdraw
        [HttpPost("{id}/withdraw")]
        public ActionResult<PrintResult> PrintWithdraw(string id, [FromBody] TransferAmount withdraw)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.PrintMoneyWithdraw(withdraw.Amount)
                };
            }
            return NotFound();
        }

        // POST printers/{id}/deposit
        [HttpPost("{id}/deposit")]
        public ActionResult<PrintResult> PrintDeposit(string id, [FromBody] TransferAmount deposit)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.PrintMoneyDeposit(deposit.Amount)
                };
            }
            return NotFound();
        }

        // POST printers/{id}/datetime
        [HttpPost("{id}/datetime")]
        public ActionResult<PrintResult> SetDateTime(string id, [FromBody] CurrentDateTime currentDateTime)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.SetDateTime(currentDateTime.DateTime)
                };
            }
            return NotFound();
        }

        // POST printers/{id}/zreport
        [HttpPost("{id}/zreport")]
        public ActionResult<PrintResult> PrintZReport(string id)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.PrintZReport()
                };
            }
            return NotFound();
        }

        // POST printers/{id}/xreport
        [HttpPost("{id}/xreport")]
        public ActionResult<PrintResult> PrintXReport(string id)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                return new PrintResult
                {
                    Status = printer.PrintXReport()
                };
            }
            return NotFound();
        }
    }
}
