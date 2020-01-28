namespace ErpNet.FP.Server.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ErpNet.FP.Core;
    using ErpNet.FP.Core.Service;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    // PrintersController, example: //host/printers/[controller]
    [Route("[controller]")]
    [ApiController]
    public class PrintersController : ControllerBase
    {
        private readonly IServiceController context;

        public PrintersController(IServiceController context)
        {
            this.context = context;
        }

        // GET /
        [HttpGet()]
        public ActionResult<Dictionary<string, DeviceInfo>> Printers()
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            return context.PrintersInfo;
        }

        // GET {id}
        [HttpGet("{id}")]
        public ActionResult<DeviceInfo> Info(string id)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.PrintersInfo.TryGetValue(id, out DeviceInfo? deviceInfo))
            {
                return deviceInfo;
            }
            return NotFound();
        }

        // GET {id}/status
        [HttpGet("{id}/status")]
        public ActionResult<DeviceStatusWithDateTime> Status(string id)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                return printer.CheckStatus();
            }
            return NotFound();
        }

        // Get {id}/cash
        [HttpGet("{id}/cash")]
        public async Task<IActionResult> Cash(
            string id,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Cash,
                        Document = null,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // GET taskinfo
        [HttpGet("taskinfo")]
        public ActionResult<TaskInfoResult> TaskInfo([FromQuery]string id)
        {
            return context.GetTaskInfo(id);
        }

        // POST {id}/rawrequest
        [HttpPost("{id}/rawrequest")]
        public async Task<IActionResult> RawRequest(
            string id,
            [FromBody] RequestFrame requestFrame,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.RawRequest,
                        Document = requestFrame,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/receipt
        [HttpPost("{id}/receipt")]
        public async Task<IActionResult> PrintReceipt(
            string id,
            [FromBody] Receipt receipt,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Receipt,
                        Document = receipt,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/reversalreceipt
        [HttpPost("{id}/reversalreceipt")]
        public async Task<IActionResult> PrintReversalReceipt(
            string id,
            [FromBody] ReversalReceipt reversalReceipt,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.ReversalReceipt,
                        Document = reversalReceipt,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/withdraw
        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> PrintWithdraw(
            string id,
            [FromBody] TransferAmount withdraw,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Withdraw,
                        Document = withdraw,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/deposit
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> PrintDeposit(
            string id,
            [FromBody] TransferAmount deposit,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Deposit,
                        Document = deposit,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/datetime
        [HttpPost("{id}/datetime")]
        public async Task<IActionResult> SetDateTime(
            string id,
            [FromBody] CurrentDateTime datetime,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.SetDateTime,
                        Document = datetime,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/zreport
        [HttpPost("{id}/zreport")]
        public async Task<IActionResult> PrintZReport(
            string id,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.ZReport,
                        Document = null,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/xreport
        [HttpPost("{id}/xreport")]
        public async Task<IActionResult> PrintXReport(
            string id,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.XReport,
                        Document = null,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/duplicate
        [HttpPost("{id}/duplicate")]
        public async Task<IActionResult> PrintDuplicate(
            string id,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Duplicate,
                        Document = null,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }

        // POST {id}/reset
        [HttpPost("{id}/reset")]
        public async Task<IActionResult> Reset(
            string id,
            [FromQuery] string? taskId,
            [FromQuery] int asyncTimeout = PrintJob.DefaultTimeout)
        {
            if (!context.IsReady)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            }
            if (context.Printers.TryGetValue(id, out IFiscalPrinter? printer))
            {
                var result = await context.RunAsync(
                    new PrintJob
                    {
                        Printer = printer,
                        Action = PrintJobAction.Reset,
                        Document = null,
                        AsyncTimeout = asyncTimeout,
                        TaskId = taskId
                    });
                return Ok(result);
            }
            return NotFound();
        }
    }
}
