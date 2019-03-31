using System.Collections.Generic;
using ErpNet.FP.Core;

namespace ErpNet.FP.Core.Drivers.BgTremol
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgZfpFiscalPrinter" />
    public partial class BgTremolZfpFiscalPrinter : BgZfpFiscalPrinter
    {
        protected override DeviceStatus ParseStatus(byte[] status)
        {
            return new DeviceStatus();

            /*
            TODO: There are two different variants of statuses in protocol
            Ack Status, and Device Status
            Parsing of two different variants is neccessary
            
            status[0] - FD Errors
            30 OK
            31 Out of paper, printer failure
            32 Registers overflow
            33 Clock failure or incorrect date & time 33 Z daily report is not zero
            34 Opened fiscal receipt 34 Syntax error
            35 Payment residue account            
            36 Opened non-fiscal receipt 36 Zero input registers
            37 Registered payment but receipt is not closed            
            38 Fiscal memory failure            
            39 Incorrect password
            3a Missing external display
            3b 24hours block – missing Z report
            3c Overheated printer thermal head.
            3d Interrupt power supply in fiscal receipt(one time until status is read)
            3e Overflow EJ
            3f Insufficient conditions
            
            status[1] - Command Errors
            30 OK
            31 Invalid command
            32 Illegal command
            33 Z daily report is not zero
            34 Syntax error
            35 Input registers overflow
            36 Zero input registers
            37 Unavailable transaction for correction
            38 Insufficient amount on hand
            */
        }

    }
}
