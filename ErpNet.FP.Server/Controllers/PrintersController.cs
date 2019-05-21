using ErpNet.FP.Core;
using ErpNet.FP.Server.Contexts;
using ErpNet.FP.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErpNet.FP.Server.Controllers
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

        // GET printers/taskinfo
        [HttpGet("taskinfo")]
        public ActionResult<TaskInfoResult> TaskInfo([FromQuery]string id) =>
            context.GetTaskInfo(id);

        // POST printers/{id}/receipt
        [HttpPost("{id}/receipt")]
        public async Task<IActionResult> PrintReceipt(
            string id,
            [FromBody] Receipt receipt,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.Receipt,
                    receipt,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/reversalreceipt
        [HttpPost("{id}/reversalreceipt")]
        public async Task<IActionResult> PrintReversalReceipt(
            string id,
            [FromBody] ReversalReceipt reversalReceipt,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.ReversalReceipt,
                    reversalReceipt,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/withdraw
        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> PrintWithdraw(
            string id,
            [FromBody] TransferAmount withdraw,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.Withdraw,
                    withdraw,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/deposit
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> PrintDeposit(
            string id,
            [FromBody] TransferAmount deposit,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.Deposit,
                    deposit,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/datetime
        [HttpPost("{id}/datetime")]
        public async Task<IActionResult> SetDateTime(
            string id,
            [FromBody] CurrentDateTime datetime,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.SetDateTime,
                    datetime,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/zreport
        [HttpPost("{id}/zreport")]
        public async Task<IActionResult> PrintZReport(
            string id,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.ZReport,
                    null,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }

        // POST printers/{id}/xreport
        [HttpPost("{id}/xreport")]
        public async Task<IActionResult> PrintXReport(
            string id,
            [FromQuery] int timeout = PrintJob.DefaultTimeout,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (context.Printers.TryGetValue(id, out IFiscalPrinter printer))
            {
                var result = await context.RunAsync(
                    printer,
                    PrintJobAction.XReport,
                    null,
                    timeout,
                    asyncTimeout);
                if (result == null)
                {
                    return NoContent(); // Timeout occured, so nothing returned
                }
                return Ok(result);
            }
            return NotFound();
        }
    }
}
